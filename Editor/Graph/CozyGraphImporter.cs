using UnityEngine;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using System;
using System.Linq;
using System.Collections.Generic;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [ScriptedImporter(1, CozyGraph.AssetExtension)]
    public class CozyGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            CozyGraph cozyGraphEditor = GraphDatabase.LoadGraphForImporter<CozyGraph>(ctx.assetPath);
            RuntimeCozyGraph cozyGraphRuntime = ScriptableObject.CreateInstance<RuntimeCozyGraph>();

            // Build a dictionary with generated GUID
            Dictionary<INode, string> ids = new();
            foreach (var node in cozyGraphEditor.GetNodes())
            { 
                ids[node] = Guid.NewGuid().ToString();
            }

            // Detect entry node from StartNode
            var start = cozyGraphEditor.GetNodes().OfType<StartNode>().FirstOrDefault();
            if (start != null)
            {
                var outPort = start.GetOutputPorts().FirstOrDefault();      // Gets the first output.
                var next = outPort?.firstConnectedPort;                     // Gets the what's connected to the first output.
                if (next != null)                                           // Then if we found a connection to the first output...
                    cozyGraphRuntime.EntryNodeID = ids[next.GetNode()];     // We set the Runtime Graph's Entry node to that node.
            }

            // For each editor nodes...
            foreach (var node in cozyGraphEditor.GetNodes())
            {
                // Excluding the Start node (This was handled previously by assigning the EntryNode property of the Editor Graph)...
                if (node is StartNode || node is EndNode)
                    continue;

                RuntimeCozyNode r;

                if (node is CozyEditorNode cen)
                {
                    r = cen.CreateRuntimeNode(ids[node], node.GetType().Name, cozyGraphRuntime);
                    Debug.Log($"HEYYYY Importer created runtime node of type: {r.GetType().FullName} for editor node {node.GetType().Name}");

                    // Convert all INPUT ports into RuntimeCozyPorts.
                    foreach (IPort port in node.GetInputPorts())
                    {
                        r.RegisterPort(port.name, GetPortValue(port, ids), false);
                    }

                    // Convert also all OUTPUT ports into RuntimeCozyPorts.
                    foreach (IPort port in node.GetOutputPorts())
                    {
                        r.RegisterPort(port.name, GetPortValue(port, ids), true);
                    }
                }
                else
                {
                    r = new RuntimeCozyNode();
                    r.NodeID = ids[node];
                    r.NodeType = node.GetType().Name;
                }

                // NEXT NODE LINK
                var outPort = node.GetOutputPorts().FirstOrDefault();
                var next = outPort?.firstConnectedPort;
                if (next != null)
                    r.NextNodeID = ids[next.GetNode()];

                cozyGraphRuntime.AllNodes.Add(r);
            }

            // Unity's Graph Toolkit way of finishing the import proccess.
            ctx.AddObjectToAsset("Runtime", cozyGraphRuntime);
            ctx.SetMainObject(cozyGraphRuntime);

            // Uncomment the following line to open the Json version of the runtimeGraph at the end of the import:
            RuntimeGraphJsonDebug.DumpToJsonAndOpen(cozyGraphRuntime);
        }

        public static object GetPortValue(IPort _port, Dictionary<INode, string> _ids)
        {
            // Se o port for inválido.
            if (_port == null) return default;
            // Caso tenha algo conectado, vamos verificar.
            if (_port.isConnected)
            {
                INode connectedNode = _port.firstConnectedPort.GetNode();

                if (connectedNode is IConstantNode cn)
                {
                    // Constant nodes in GraphToolkit store their value directly on the output port
                    // cn.TryGetValue(out object value);
                    return new RuntimeIConstant(_ids[_port.GetNode()], cn.dataType.ToString());
                }

                if (connectedNode is IVariableNode vn)
                {
                    return new RuntimeIVariable(_ids[_port.GetNode()], vn.variable.ToString());
                }

                //  if (connectedNode is CozyEditorNode) */
                // {
                    RuntimeIPort portValue = new(_ids[_port.GetNode()], _port.firstConnectedPort.name);
                    // portValue.
                    return portValue;
                // }

                // string[] nodeNameSplit = _port.GetNode().ToString().Split(".");
                // string nodeName = nodeNameSplit[nodeNameSplit.Length -1];
                // 
                // return $"Could not determine the value of { nodeName }'s {_port.name } port.";
            }
            else
            {
                _port.TryGetValue(out object result);
                return result;
            }
        }
    }
}
