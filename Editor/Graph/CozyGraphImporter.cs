using System;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using ShaderFactory.CozyNodes.Runtime;

namespace ShaderFactory.CozyNodes.Editor
{
    // Increment this version whenever imported runtime data changes. Unity will then
    // rebuild existing .cozygraph assets instead of running stale runtime data.
    [ScriptedImporter(41, CozyGraph.AssetExtension)]
    public class CozyGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            RuntimeCozyGraph rootRuntimeGraph = ScriptableObject.CreateInstance<RuntimeCozyGraph>();
            CozyGraph rootEditorGraph = GraphDatabase.LoadGraphForImporter<CozyGraph>(ctx.assetPath);
            if (rootEditorGraph == null)
            {
                Debug.LogError(
                    $"Could not load Cozy Graph '{ctx.assetPath}'. The graph asset must be migrated " +
                    "to the Unity 6.6 Graph Toolkit format before it can be imported.");
                ctx.AddObjectToAsset("Runtime", rootRuntimeGraph);
                ctx.SetMainObject(rootRuntimeGraph);
                return;
            }

            ImportContext importContext = new ImportContext(ctx);
            importContext.RegisterRoot(rootEditorGraph, rootRuntimeGraph);
            ImportGraph(rootEditorGraph, rootRuntimeGraph, importContext);
            importContext.CompleteGraph(rootEditorGraph);

            ctx.AddObjectToAsset("Runtime", rootRuntimeGraph);
            ctx.SetMainObject(rootRuntimeGraph);
        }

        /// <summary>
        /// Imports an authoring graph into one runtime graph object. The same method
        /// handles the root and local children so ports, entries and node identities
        /// cannot drift between the two paths.
        /// </summary>
        private static void ImportGraph(
            Graph editorGraph,
            RuntimeCozyGraph runtimeGraph,
            ImportContext importContext)
        {
            runtimeGraph.SourceGraphID = editorGraph.ID.ToString();
            Dictionary<INode, string> nodeIDs = importContext.GetNodeIDs(editorGraph);

            ImportVariableDefinitions(editorGraph, runtimeGraph);

            foreach (INode editorNode in editorGraph.GetNodes())
            {
                // Block nodes are imported by their owning context. Graph Toolkit
                // can include them in GetNodes depending on the graph version,
                // but blocks must never become independent runtime flow nodes.
                if (editorNode is CustomCozyBlock)
                    continue;

                if (editorNode is CustomCozyContext customCozyContext)
                {
                    ImportContextNode(customCozyContext, runtimeGraph, nodeIDs);
                    continue;
                }

                if (editorNode is CustomCozyNode customEntryNode &&
                    TryImportEntry(customEntryNode, nodeIDs, runtimeGraph))
                {
                    continue;
                }

                if (CozyGraphToolkitSubgraphInterface.TryGetFlowOutputGateway(
                        editorNode,
                        out string flowOutputName,
                        out _))
                {
                    runtimeGraph.AllNodes.Add(new RuntimeCozySubgraphExitNode
                    {
                        NodeID = nodeIDs[editorNode],
                        NodeType = editorNode.GetType().Name,
                        Graph = runtimeGraph,
                        ParentFlowOutputName = flowOutputName
                    });
                    continue;
                }

                if (editorNode is ISubgraphNode)
                {
                    ImportSubgraphNode(
                        editorNode,
                        editorGraph,
                        runtimeGraph,
                        importContext);
                    continue;
                }

                if (editorNode is CustomCozyNode customCozyNode &&
                    customCozyNode.HasUnresolvedTypeParameters)
                {
                    Debug.LogError(
                        $"Cannot import '{customCozyNode.Title}' because one or more " +
                        "type parameters have no selected type. Choose a type in the node UI.");
                    continue;
                }

                // Helper nodes store local data directly on the receiving port.
                if (editorNode is IConstantNode || editorNode is IVariableNode)
                    continue;

                if (editorNode is not CozyEditorNode cozyEditorNode)
                {
                    Debug.LogError(
                        $"Editor node {StringHelper.RemoveHierarchyFromClassName(editorNode.ToString())} " +
                        "must derive from CozyEditorNode to be imported.");
                    continue;
                }

                RuntimeCozyNode runtimeNode = cozyEditorNode.CreateRuntimeNode(
                    nodeIDs[editorNode],
                    editorNode.GetType().Name,
                    runtimeGraph);

                ImportDataPorts(editorNode, cozyEditorNode, runtimeNode, nodeIDs);
                ImportFlowOutputs(editorNode, cozyEditorNode, runtimeNode, nodeIDs);
                runtimeGraph.AllNodes.Add(runtimeNode);
            }
        }

        private static void ImportContextNode(
            CustomCozyContext editorContext,
            RuntimeCozyGraph runtimeGraph,
            Dictionary<INode, string> nodeIDs)
        {
            if (!nodeIDs.TryGetValue(editorContext, out string contextID))
            {
                Debug.LogError($"Context '{editorContext.Title}' has no stable graph ID.");
                return;
            }

            CozyNodeDefinition contextDefinition = CozyNodeReflection.GetDefinition(
                editorContext.ResolveCustomContextType());
            if (contextDefinition == null)
            {
                Debug.LogError($"Context '{editorContext.Title}' has no valid authored type.");
                return;
            }

            RuntimeCozyContextNode runtimeContext = editorContext.CreateRuntimeNode(
                contextID,
                editorContext.GetType().Name,
                runtimeGraph);
            ImportAuthoredElementDataPorts(
                editorContext,
                contextDefinition,
                runtimeContext,
                nodeIDs);
            ImportAuthoredElementFlowOutputs(
                editorContext,
                contextDefinition,
                runtimeContext,
                nodeIDs);

            foreach (BlockNode editorBlock in editorContext.BlockNodes)
            {
                if (editorBlock is not CustomCozyBlock customCozyBlock)
                    continue;

                if (!nodeIDs.TryGetValue(customCozyBlock, out string blockID))
                {
                    Debug.LogError(
                        $"Block '{customCozyBlock.Title}' in context '{editorContext.Title}' " +
                        "has no stable graph ID.");
                    continue;
                }

                CozyNodeDefinition blockDefinition = CozyNodeReflection.GetDefinition(
                    customCozyBlock.ResolveCustomBlockType());
                if (blockDefinition == null)
                {
                    Debug.LogError(
                        $"Block '{customCozyBlock.Title}' in context '{editorContext.Title}' " +
                        "has no valid authored type.");
                    continue;
                }

                RuntimeCozyBlock runtimeBlock = customCozyBlock.CreateRuntimeBlock(
                    blockID,
                    customCozyBlock.GetType().Name,
                    runtimeGraph);
                ImportAuthoredElementDataPorts(
                    customCozyBlock,
                    blockDefinition,
                    runtimeBlock,
                    nodeIDs);
                ImportAuthoredElementFlowOutputs(
                    customCozyBlock,
                    blockDefinition,
                    runtimeBlock,
                    nodeIDs);

                // Block outputs leave through the owning context at runtime.
                // Preserve each external wire on a collision-free context key;
                // the context runner chooses that key only when this block asks
                // to exit through the matching authored output name.
                foreach (RuntimeFlowConnection flowConnection in runtimeBlock.FlowOutputConnections)
                {
                    runtimeContext.RegisterFlowOutput(
                        RuntimeCozyContextNode.GetBlockFlowOutputKey(
                            runtimeBlock.NodeID,
                            flowConnection.OutputPortName),
                        flowConnection.NextNodeID,
                        flowConnection.NextInputPortName);
                }

                runtimeContext.Blocks.Add(runtimeBlock);
            }

            runtimeGraph.AllNodes.Add(runtimeContext);
        }

        private static void ImportAuthoredElementDataPorts(
            INode editorNode,
            CozyNodeDefinition definition,
            RuntimeCozyAuthoredElement runtimeElement,
            Dictionary<INode, string> nodeIDs)
        {
            foreach (IPort port in editorNode.GetInputPorts())
            {
                if (definition.FlowInputs.Contains(port.Name))
                    continue;

                runtimeElement.RegisterPort(
                    port.Name,
                    CozyPortValueImporter.GetPortValue(port, nodeIDs),
                    false,
                    port.DataType);
            }

            foreach (IPort port in editorNode.GetOutputPorts())
            {
                if (definition.FlowOutputs.Contains(port.Name))
                    continue;

                runtimeElement.RegisterPort(
                    port.Name,
                    CozyPortValueImporter.GetPortValue(port, nodeIDs),
                    true,
                    port.DataType);
            }
        }

        private static void ImportAuthoredElementFlowOutputs(
            INode editorNode,
            CozyNodeDefinition definition,
            RuntimeCozyAuthoredElement runtimeElement,
            Dictionary<INode, string> nodeIDs)
        {
            foreach (IPort outputPort in editorNode.GetOutputPorts())
            {
                if (definition.FlowOutputs.Contains(outputPort.Name))
                    RegisterFlowOutput(runtimeElement, outputPort, nodeIDs);
            }
        }

        private static void ImportVariableDefinitions(
            Graph editorGraph,
            RuntimeCozyGraph runtimeGraph)
        {
            HashSet<string> flowGatewayNames =
                CozyGraphToolkitSubgraphInterface.GetFlowGatewayNames(editorGraph);

            foreach (IVariable editorVariable in editorGraph.GetVariables())
            {
                // GTK represents subgraph flow boundaries as variables too. The
                // child connection determines whether a declaration is flow.
                if (editorVariable == null ||
                    flowGatewayNames.Contains(editorVariable.Name))
                    continue;

                RuntimeCozyVariableDefinition runtimeDefinition =
                    new RuntimeCozyVariableDefinition
                    {
                        VariableID = editorVariable.ID.ToString(),
                        Name = editorVariable.Name,
                        ValueTypeName = editorVariable.DataType?.AssemblyQualifiedName
                    };

                object defaultValue = CozyPortValueImporter.GetVariableDefaultValue(
                    editorVariable,
                    editorVariable.DataType);
                runtimeDefinition.DefaultValue.SetValue(defaultValue, editorVariable.DataType);
                runtimeGraph.VariableDefinitions.Add(runtimeDefinition);
            }
        }

        private static void ImportDataPorts(
            INode editorNode,
            CozyEditorNode cozyEditorNode,
            RuntimeCozyNode runtimeNode,
            Dictionary<INode, string> nodeIDs)
        {
            foreach (IPort port in editorNode.GetInputPorts())
            {
                if (cozyEditorNode.IsFlowInputPort(port.Name))
                    continue;

                Type portValueType = editorNode is CustomCozyNode customNode
                    ? customNode.GetResolvedPortValueType(port)
                    : port.DataType;
                runtimeNode.RegisterPort(
                    port.Name,
                    CozyPortValueImporter.GetPortValue(port, nodeIDs),
                    false,
                    portValueType);
            }

            foreach (IPort port in editorNode.GetOutputPorts())
            {
                if (cozyEditorNode.IsFlowOutputPort(port.Name))
                    continue;

                Type portValueType = editorNode is CustomCozyNode customNode
                    ? customNode.GetResolvedPortValueType(port)
                    : port.DataType;
                runtimeNode.RegisterPort(
                    port.Name,
                    CozyPortValueImporter.GetPortValue(port, nodeIDs),
                    true,
                    portValueType);
            }
        }

        private static void ImportFlowOutputs(
            INode editorNode,
            CozyEditorNode cozyEditorNode,
            RuntimeCozyNode runtimeNode,
            Dictionary<INode, string> nodeIDs)
        {
            foreach (IPort outputPort in editorNode.GetOutputPorts())
            {
                if (cozyEditorNode.IsFlowOutputPort(outputPort.Name))
                    RegisterFlowOutput(runtimeNode, outputPort, nodeIDs);
            }
        }

        private static void ImportSubgraphNode(
            INode editorNode,
            Graph parentEditorGraph,
            RuntimeCozyGraph parentRuntimeGraph,
            ImportContext importContext)
        {
            if (!CozyGraphToolkitSubgraphInterface.TryDescribe(
                    editorNode,
                    out CozyGraphToolkitSubgraphInterface.Description description,
                    out string interfaceError))
            {
                Debug.LogError(
                    $"Could not import subgraph '{editorNode.Title}': {interfaceError}");
                return;
            }

            if (!CozyGraphToolkitSubgraphInterface.IsLocalSubgraphNode(editorNode))
            {
                Debug.LogError(
                    $"Subgraph '{editorNode.Title}' is an external graph asset. " +
                    "This Cozy Nodes phase supports local subgraphs only.");
                return;
            }

            RuntimeCozyGraph childRuntimeGraph = importContext.GetOrImportSubgraph(
                description.Subgraph);
            if (childRuntimeGraph == null)
                return;

            RuntimeCozySubgraphNode runtimeNode = new RuntimeCozySubgraphNode
            {
                NodeID = importContext.GetNodeID(parentEditorGraph, editorNode),
                NodeType = editorNode.GetType().Name,
                Graph = parentRuntimeGraph,
                Subgraph = childRuntimeGraph
            };

            foreach (CozyGraphToolkitSubgraphInterface.DataPort dataInput in description.DataInputs)
            {
                Type valueType = dataInput.Variable.DataType;
                runtimeNode.RegisterPort(
                    dataInput.ParentPort.Name,
                    CozyPortValueImporter.GetPortValue(
                        dataInput.ParentPort,
                        importContext.GetNodeIDs(parentEditorGraph)),
                    false,
                    valueType);
                runtimeNode.DataInputs.Add(new RuntimeCozySubgraphDataInput
                {
                    ParentPortName = dataInput.ParentPort.Name,
                    ChildVariableID = dataInput.Variable.ID.ToString(),
                    ValueTypeName = valueType.AssemblyQualifiedName
                });
            }

            foreach (CozyGraphToolkitSubgraphInterface.DataPort dataOutput in description.DataOutputs)
            {
                Type valueType = dataOutput.Variable.DataType;
                runtimeNode.RegisterPort(
                    dataOutput.ParentPort.Name,
                    null,
                    true,
                    valueType);
                RuntimeCozySubgraphDataOutput runtimeDataOutput =
                    new RuntimeCozySubgraphDataOutput
                {
                    ParentPortName = dataOutput.ParentPort.Name,
                    ChildVariableID = dataOutput.Variable.ID.ToString(),
                    ValueTypeName = valueType.AssemblyQualifiedName
                };

                if (CozyGraphToolkitSubgraphInterface.TryGetDataOutputSource(
                        description.Subgraph,
                        dataOutput.Variable,
                        out IPort childOutputSource))
                {
                    runtimeDataOutput.HasConnectedSource = true;
                    runtimeDataOutput.Source.SetValue(
                        CozyPortValueImporter.GetPortValue(
                            childOutputSource,
                            importContext.GetNodeIDs(description.Subgraph)),
                        valueType);
                    runtimeDataOutput.Source.ValueTypeName = valueType.AssemblyQualifiedName;
                }

                runtimeNode.DataOutputs.Add(runtimeDataOutput);
            }

            foreach (CozyGraphToolkitSubgraphInterface.FlowPort flowInput in description.FlowInputs)
            {
                IPort firstChildPort = flowInput.ChildPort.FirstConnectedPort;
                if (firstChildPort == null)
                {
                    Debug.LogError(
                        $"Subgraph '{editorNode.Title}' flow input '{flowInput.Name}' " +
                        "is not connected inside the child graph.");
                    return;
                }

                if (!importContext.TryGetNodeID(
                        description.Subgraph,
                        firstChildPort.GetNode(),
                        out string targetNodeID))
                {
                    Debug.LogError(
                        $"Subgraph '{editorNode.Title}' flow input '{flowInput.Name}' " +
                        "does not lead to an importable child node.");
                    return;
                }

                runtimeNode.FlowEntries.Add(new RuntimeCozySubgraphFlowEntry
                {
                    ParentInputPortName = flowInput.ParentPort.Name,
                    FlowInputName = flowInput.Name,
                    TargetNodeID = targetNodeID,
                    TargetInputPortName = firstChildPort.Name
                });
            }

            foreach (CozyGraphToolkitSubgraphInterface.FlowPort flowOutput in description.FlowOutputs)
            {
                RegisterFlowOutput(
                    runtimeNode,
                    flowOutput.ParentPort,
                    importContext.GetNodeIDs(parentEditorGraph),
                    flowOutput.Name);
            }

            parentRuntimeGraph.AllNodes.Add(runtimeNode);
        }

        private static void RegisterFlowOutput(
            RuntimeCozyNode runtimeNode,
            IPort outputPort,
            Dictionary<INode, string> nodeIDs,
            string runtimeOutputName = null)
        {
            IPort connectedPort = outputPort?.FirstConnectedPort;
            if (connectedPort == null)
                return;

            if (!nodeIDs.TryGetValue(connectedPort.GetNode(), out string nextNodeID))
            {
                Debug.LogError(
                    $"Flow output '{outputPort.DisplayName}' points to a node that cannot be imported.");
                return;
            }

            runtimeNode.RegisterFlowOutput(
                string.IsNullOrWhiteSpace(runtimeOutputName)
                    ? outputPort.Name
                    : runtimeOutputName,
                nextNodeID,
                connectedPort.Name);
        }

        private static bool TryImportEntry(
            CustomCozyNode editorNode,
            Dictionary<INode, string> nodeIDs,
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
                    $"On Event on '{definition.Title}' needs an Event Name.");
                return true;
            }

            IPort outputPort = editorNode.GetOutputPortByName(
                entryDefinition.FlowOutputName);
            IPort connectedPort = outputPort?.FirstConnectedPort;
            if (connectedPort == null)
            {
                Debug.LogWarning(
                    $"Entry '{GetEntryDisplayName(entryDefinition, entryName)}' " +
                    "has no connected flow output.");
                return true;
            }

            if (!nodeIDs.TryGetValue(connectedPort.GetNode(), out string targetNodeID))
            {
                Debug.LogError(
                    $"Entry '{GetEntryDisplayName(entryDefinition, entryName)}' " +
                    "points to a node that cannot be imported.");
                return true;
            }

            if (HasDuplicateEntry(runtimeGraph, entryDefinition.Kind, entryName))
            {
                Debug.LogError(
                    $"The graph already has an entry named " +
                    $"'{GetEntryDisplayName(entryDefinition, entryName)}'.");
                return true;
            }

            runtimeGraph.Entries.Add(new RuntimeCozyGraphEntry
            {
                Kind = entryDefinition.Kind,
                Name = entryName,
                TargetNodeID = targetNodeID,
                TargetInputPortName = connectedPort.Name
            });
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
                    string.Equals(existingEntry.Name, entryName, StringComparison.Ordinal))
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

        /// <summary>
        /// Owns one imported runtime object per authored graph. Local subgraphs are
        /// added as sub-assets of the root .cozygraph asset so normal Unity object
        /// references serialize safely; recursive local subgraphs are rejected.
        /// </summary>
        private sealed class ImportContext
        {
            private sealed class ImportedGraph
            {
                internal RuntimeCozyGraph RuntimeGraph;
                internal Dictionary<INode, string> NodeIDs;
                internal bool IsImporting;
            }

            private readonly AssetImportContext assetImportContext;
            private readonly Dictionary<Graph, ImportedGraph> importedGraphs =
                new Dictionary<Graph, ImportedGraph>();
            private int childGraphCount;

            internal ImportContext(AssetImportContext assetImportContext)
            {
                this.assetImportContext = assetImportContext;
            }

            internal void RegisterRoot(Graph rootEditorGraph, RuntimeCozyGraph rootRuntimeGraph)
            {
                importedGraphs.Add(
                    rootEditorGraph,
                    CreateImportedGraph(rootEditorGraph, rootRuntimeGraph));
            }

            internal void CompleteGraph(Graph editorGraph)
            {
                if (importedGraphs.TryGetValue(editorGraph, out ImportedGraph importedGraph))
                    importedGraph.IsImporting = false;
            }

            internal RuntimeCozyGraph GetOrImportSubgraph(Graph editorGraph)
            {
                if (importedGraphs.TryGetValue(editorGraph, out ImportedGraph existingGraph))
                {
                    if (existingGraph.IsImporting)
                    {
                        Debug.LogError(
                            $"Local subgraph '{editorGraph.Name}' recursively references itself. " +
                            "Recursive subgraphs are not supported.");
                        return null;
                    }

                    return existingGraph.RuntimeGraph;
                }

                RuntimeCozyGraph childRuntimeGraph = ScriptableObject.CreateInstance<RuntimeCozyGraph>();
                childRuntimeGraph.name = editorGraph.Name;
                assetImportContext.AddObjectToAsset(
                    "Subgraph_" + childGraphCount++,
                    childRuntimeGraph);

                importedGraphs.Add(
                    editorGraph,
                    CreateImportedGraph(editorGraph, childRuntimeGraph));
                ImportGraph(editorGraph, childRuntimeGraph, this);
                CompleteGraph(editorGraph);
                return childRuntimeGraph;
            }

            internal Dictionary<INode, string> GetNodeIDs(Graph editorGraph)
            {
                return importedGraphs[editorGraph].NodeIDs;
            }

            internal string GetNodeID(Graph editorGraph, INode node)
            {
                return GetNodeIDs(editorGraph)[node];
            }

            internal bool TryGetNodeID(Graph editorGraph, INode node, out string nodeID)
            {
                return GetNodeIDs(editorGraph).TryGetValue(node, out nodeID);
            }

            private static ImportedGraph CreateImportedGraph(
                Graph editorGraph,
                RuntimeCozyGraph runtimeGraph)
            {
                Dictionary<INode, string> nodeIDs = new Dictionary<INode, string>();
                foreach (INode node in editorGraph.GetNodes())
                {
                    nodeIDs[node] = node.ID.ToString();

                    if (node is not ContextNode contextNode)
                        continue;

                    foreach (BlockNode blockNode in contextNode.BlockNodes)
                        nodeIDs[blockNode] = blockNode.ID.ToString();
                }

                return new ImportedGraph
                {
                    RuntimeGraph = runtimeGraph,
                    NodeIDs = nodeIDs,
                    IsImporting = true
                };
            }
        }
    }
}
