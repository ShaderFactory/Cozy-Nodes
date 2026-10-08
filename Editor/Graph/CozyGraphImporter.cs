using UnityEngine;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using System.Linq;
using System.Collections.Generic;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    // Increment this version whenever imported runtime data changes. Unity will then
    // rebuild existing .cozygraph assets instead of running stale runtime data.
    [ScriptedImporter(17, CozyGraph.AssetExtension)]
    public class CozyGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogWarning("Started Import Proccess.");

            // Unity's Graph Toolkit way of starting the import proccess.
            RuntimeCozyGraph cozyGraphRuntime = ScriptableObject.CreateInstance<RuntimeCozyGraph>();
            CozyGraph cozyGraphEditor = GraphDatabase.LoadGraphForImporter<CozyGraph>(ctx.assetPath);
            if (cozyGraphEditor == null)
            {
                Debug.LogError($"Could not load Cozy Graph '{ctx.assetPath}'. The graph asset must be migrated to the Unity 6.6 Graph Toolkit format before it can be imported.");
                ctx.AddObjectToAsset("Runtime", cozyGraphRuntime);
                ctx.SetMainObject(cozyGraphRuntime);
                return;
            }

            // Preserve Graph Toolkit's stable node identifiers. The runtime uses the
            // same identities after every import, which is important for debugging,
            // future save data and references between imported nodes.
            Dictionary<INode, string> nodesIdDictionary = new();
            foreach (var node in cozyGraphEditor.GetNodes())
            {
                nodesIdDictionary[node] = node.ID.ToString();
            }

            // The Blackboard defines the initial state of a graph. Import these
            // declarations separately from variable nodes, which only reference them.
            foreach (IVariable editorVariable in cozyGraphEditor.GetVariables())
            {
                RuntimeCozyVariableDefinition runtimeDefinition = new RuntimeCozyVariableDefinition();
                runtimeDefinition.VariableID = editorVariable.ID.ToString();
                runtimeDefinition.Name = editorVariable.Name;
                if (editorVariable.DataType != null)
                    runtimeDefinition.ValueTypeName = editorVariable.DataType.AssemblyQualifiedName;

                object defaultValue = CozyPortValueImporter.GetVariableDefaultValue(
                    editorVariable,
                    editorVariable.DataType);
                runtimeDefinition.DefaultValue.SetValue(defaultValue);

                cozyGraphRuntime.VariableDefinitions.Add(runtimeDefinition);
            }

            foreach (var editorNode in cozyGraphEditor.GetNodes())
            {
                // Start is only a graph entry point. It does not need a runtime node,
                // because execution begins at the first node connected to it.
                if (editorNode is StartNode)
                {
                    IPort startPort = editorNode.GetOutputPorts().FirstOrDefault();
                    IPort firstConnectedPort = startPort?.FirstConnectedPort;
                    if (firstConnectedPort != null)
                    {
                        cozyGraphRuntime.EntryNodeID = nodesIdDictionary[firstConnectedPort.GetNode()];
                    }

                    continue;
                }

                // Graph Toolkit creates these helper nodes for values stored directly in
                // the graph. Their values are copied when an input port is imported, so
                // they do not need their own RuntimeCozyNode.
                if (editorNode is IConstantNode || editorNode is IVariableNode)
                    continue;

                // Only CozyEditorNode types know how to create their runtime equivalent.
                if (editorNode is not CozyEditorNode cozyEditorNode)
                {
                    Debug.LogError($"Editor node {StringHelper.RemoveHierarchyFromClassName(editorNode.ToString())} must derive from CozyEditorNode to be imported.");
                    continue;
                }

                string runtimeNodeID = nodesIdDictionary[editorNode];
                string runtimeNodeType = editorNode.GetType().Name;
                RuntimeCozyNode runtimeNode = cozyEditorNode.CreateRuntimeNode(
                    runtimeNodeID,
                    runtimeNodeType,
                    cozyGraphRuntime);

                // Copy only data ports into serializable runtime ports. Flow ports are
                // stored below as named paths instead of values.
                foreach (IPort port in editorNode.GetInputPorts())
                {
                    if (!cozyEditorNode.IsFlowInputPort(port.Name))
                        runtimeNode.RegisterPort(
                            port.Name,
                            CozyPortValueImporter.GetPortValue(port, nodesIdDictionary),
                            false,
                            port.DataType);
                }

                foreach (IPort port in editorNode.GetOutputPorts())
                {
                    if (!cozyEditorNode.IsFlowOutputPort(port.Name))
                        runtimeNode.RegisterPort(
                            port.Name,
                            CozyPortValueImporter.GetPortValue(port, nodesIdDictionary),
                            true,
                            port.DataType);
                }

                // Import every connected flow output. A node can now choose which
                // named output to follow instead of being limited to one "next" node.
                foreach (IPort outputPort in editorNode.GetOutputPorts())
                {
                    if (!cozyEditorNode.IsFlowOutputPort(outputPort.Name))
                        continue;

                    IPort connectedPort = outputPort.FirstConnectedPort;
                    if (connectedPort != null)
                    {
                        runtimeNode.RegisterFlowOutput(
                            outputPort.Name,
                            nodesIdDictionary[connectedPort.GetNode()]);
                    }
                }

                // CozyManager looks up nodes from this list when the game starts.
                cozyGraphRuntime.AllNodes.Add(runtimeNode);

            }

            // Unity's Graph Toolkit way of finishing the import proccess.
            ctx.AddObjectToAsset("Runtime", cozyGraphRuntime);
            ctx.SetMainObject(cozyGraphRuntime);

            Debug.LogWarning("Ended Import Proccess.");

            // Uncomment the following line to open the Json version of the runtimeGraph at the end of the import:
            // RuntimeGraphJsonDebug.DumpToJsonAndOpen(cozyGraphRuntime);
        }

    }
}
