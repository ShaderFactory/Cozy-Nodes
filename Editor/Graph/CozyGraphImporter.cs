using UnityEngine;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using System.Linq;
using System.Collections.Generic;
using ShaderFactory.CozyNodes.Runtime;

namespace ShaderFactory.CozyNodes.Editor
{
    // Increment this version whenever imported runtime data changes. Unity will then
    // rebuild existing .cozygraph assets instead of running stale runtime data.
    [ScriptedImporter(27, CozyGraph.AssetExtension)]
    public class CozyGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {

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

            // Keep the authoring graph's stable identity alongside the imported
            // runtime graph. This is the bridge used by the Editor-only execution
            // visualizer to animate the node that is currently running.
            cozyGraphRuntime.SourceGraphID = cozyGraphEditor.ID.ToString();

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
                // Entry nodes are authoring markers, not runtime nodes. Their
                // connected flow target is saved separately in the runtime graph.
                if (editorNode is CustomCozyNode customEntryNode &&
                    TryImportEntry(
                        customEntryNode,
                        nodesIdDictionary,
                        cozyGraphRuntime))
                {
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
                            nodesIdDictionary[connectedPort.GetNode()],
                            connectedPort.Name);
                    }
                }

                // CozyManager looks up nodes from this list when the game starts.
                cozyGraphRuntime.AllNodes.Add(runtimeNode);

            }

            // Unity's Graph Toolkit way of finishing the import proccess.
            ctx.AddObjectToAsset("Runtime", cozyGraphRuntime);
            ctx.SetMainObject(cozyGraphRuntime);


            // Uncomment the following line to open the Json version of the runtimeGraph at the end of the import:
            // RuntimeGraphJsonDebug.DumpToJsonAndOpen(cozyGraphRuntime);
        }

        private static bool TryImportEntry(
            CustomCozyNode editorNode,
            Dictionary<INode, string> nodesIdDictionary,
            RuntimeCozyGraph runtimeGraph)
        {
            CozyNodeDefinition definition = CozyNodeReflection.GetDefinition(
                editorNode.ResolveCustomNodeType());
            CozyGraphEntryDefinition entryDefinition = definition?.Entry;
            if (entryDefinition == null)
                return false;

            string entryName = entryDefinition.Name;
            if (!string.IsNullOrWhiteSpace(entryDefinition.NameOptionName))
                entryName = editorNode.GetConfiguredOptionValue(
                    entryDefinition.NameOptionName) as string;

            if (entryDefinition.Kind == CozyGraphEntryKind.CustomEvent &&
                string.IsNullOrWhiteSpace(entryName))
            {
                Debug.LogError(
                    $"Custom Event on '{definition.Title}' needs an Event Name.");
                return true;
            }

            IPort outputPort = editorNode.GetOutputPortByName(
                entryDefinition.FlowOutputName);
            IPort connectedPort = outputPort?.FirstConnectedPort;
            if (connectedPort == null)
            {
                Debug.LogWarning(
                    $"Entry '{GetEntryDisplayName(entryDefinition, entryName)}' has no connected flow output.");
                return true;
            }

            if (HasDuplicateEntry(runtimeGraph, entryDefinition.Kind, entryName))
            {
                Debug.LogError(
                    $"The graph already has an entry named " +
                    $"'{GetEntryDisplayName(entryDefinition, entryName)}'.");
                return true;
            }

            RuntimeCozyGraphEntry entry = new RuntimeCozyGraphEntry();
            entry.Kind = entryDefinition.Kind;
            entry.Name = entryName;
            entry.TargetNodeID = nodesIdDictionary[connectedPort.GetNode()];
            entry.TargetInputPortName = connectedPort.Name;
            runtimeGraph.Entries.Add(entry);
            return true;
        }

        private static bool HasDuplicateEntry(
            RuntimeCozyGraph runtimeGraph,
            CozyGraphEntryKind kind,
            string entryName)
        {
            foreach (RuntimeCozyGraphEntry existingEntry in runtimeGraph.Entries)
            {
                if (existingEntry == null || existingEntry.Kind != kind)
                    continue;

                if (kind != CozyGraphEntryKind.CustomEvent ||
                    string.Equals(existingEntry.Name, entryName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetEntryDisplayName(
            CozyGraphEntryDefinition definition,
            string entryName)
        {
            return definition.Kind == CozyGraphEntryKind.CustomEvent
                ? entryName
                : definition.Name;
        }

    }
}
