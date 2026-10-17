using UnityEngine;
using UnityEditor;
using Unity.GraphToolkit.Editor;
using System;
using System.Collections.Generic;
using ShaderFactory.CozyNodes.Runtime;

namespace ShaderFactory.CozyNodes.Editor
{
    [Serializable]
    // Do not automatically add every Node class from this assembly to the
    // creation menu. A Cozy node must explicitly opt in with UseWithGraph,
    // which keeps editor infrastructure out of the user's node library.
    [Graph(
        AssetExtension,
        GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly |
        GraphOptions.SupportsSubgraphs)]
    public class CozyGraph : Graph
    {
        public const string AssetExtension = "cozygraph";

        // The creation menu sets this only for the graph Unity is about to create.
        // It keeps existing graphs untouched when they are opened or reimported.
        private static bool createStarterNodesForNextGraph;

        [SerializeField]
        private bool starterNodesCreated;

        [NonSerialized]
        private bool starterNodesCreationQueued;

        // Keep Cozy Tools as its own group, immediately before Unity's built-in 2D group.
        [MenuItem("Assets/Create/Cozy Tools/Cozy Graph", false, 10)]
        private static void CreateAssetFile()
        {
            createStarterNodesForNextGraph = true;
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<CozyGraph>();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (!createStarterNodesForNextGraph || starterNodesCreated || starterNodesCreationQueued)
                return;

            createStarterNodesForNextGraph = false;
            starterNodesCreationQueued = true;

            // Graph Toolkit does not allow graph changes during OnEnable. Run once
            // immediately afterwards, when Unity has finished creating the asset.
            EditorApplication.delayCall += CreateStarterNodes;
        }

        private void CreateStarterNodes()
        {
            starterNodesCreationQueued = false;

            if (starterNodesCreated || NodeCount > 0)
                return;

            UndoBeginRecordGraph("Create Cozy Graph");

            try
            {
                CustomCozyNode printNode = CreateGeneratedNode(typeof(PrintNode));
                if (printNode == null)
                {
                    Debug.LogError(
                        "Cozy Nodes could not create the starter Print node. " +
                        "Wait for Cozy Nodes to finish generating its adapters, then create a new graph.");
                    return;
                }

                CustomCozyNode startNode = CreateGeneratedNode(typeof(OnStartNode));
                if (startNode == null)
                {
                    Debug.LogError(
                        "Cozy Nodes could not create the starter On Start node. " +
                        "Wait for Cozy Nodes to finish generating its adapters, then create a new graph.");
                    return;
                }

                AddNode(startNode);
                AddNode(printNode);

                startNode.Position = new Vector2(-250f, 0f);
                printNode.Position = new Vector2(50f, 0f);
                printNode.GetInputPortByName("Message").TrySetValue("Cozy Graph is running.");

                Connect(
                    startNode.GetOutputPortByName("Execute"),
                    printNode.GetInputPortByName("in"));

                starterNodesCreated = true;
            }
            finally
            {
                UndoEndRecordGraph();
            }

            GraphDatabase.SaveGraph(this);
        }

        private static CustomCozyNode CreateGeneratedNode(Type customNodeType)
        {
            foreach (Type adapterType in TypeCache.GetTypesDerivedFrom<CustomCozyNode>())
            {
                if (adapterType.IsAbstract)
                    continue;

                if (Activator.CreateInstance(adapterType) is not CustomCozyNode adapter)
                    continue;

                if (adapter.ResolveCustomNodeType() == customNodeType)
                    return adapter;
            }

            return null;
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);

            // An option can change an InputList's shape. Rebuild only the custom
            // nodes whose stored option value now describes a different port set.
            // DefineNode preserves unchanged port names, values and wires.
            foreach (INode node in GetNodes())
            {
                if (node is CustomCozyNode customNode)
                    customNode.RefreshInputListPorts();
            }

            // Validate all nodes.
            foreach (var node in GetNodes())
            {
                CheckMultipleConnectionWarning(node, graphLogger);
                CheckBlackboardVariableInputs(node, graphLogger);
            }
        }

        /// <summary>
        /// Keeps ports strongly typed while allowing only Cozy's explicitly safe
        /// conversions, such as integer to text for a Print Node. Graph Toolkit still
        /// owns all normal same-type connection rules.
        /// </summary>
        public override bool IsConnectionAllowed(IPort firstPort, IPort secondPort)
        {
            if (firstPort == null || secondPort == null)
                return false;

            IPort inputPort = firstPort.Direction == PortDirection.Input ? firstPort : secondPort;
            IPort outputPort = firstPort.Direction == PortDirection.Output ? firstPort : secondPort;
            if (inputPort.Direction != PortDirection.Input ||
                outputPort.Direction != PortDirection.Output)
            {
                return false;
            }

            // A Cozy value input represents one value. Repeated values use an
            // InputList, which creates one port per element, rather than multiple
            // competing wires into the same port.
            if (inputPort.IsConnected)
            {
                List<IPort> connectedPorts = new List<IPort>();
                inputPort.GetConnectedPorts(connectedPorts);
                foreach (IPort connectedPort in connectedPorts)
                {
                    // GTK asks whether an existing wire is still valid after a
                    // graph change. Keep that wire valid; reject only a new,
                    // competing source.
                    if (connectedPort != outputPort)
                        return false;
                }
            }

            if (firstPort.GetNode() is CustomCozyNode firstVariableNode &&
                firstVariableNode.RequiresBlackboardVariablePort(firstPort.Name) &&
                secondPort.GetNode() is not IVariableNode)
            {
                return false;
            }

            if (secondPort.GetNode() is CustomCozyNode secondVariableNode &&
                secondVariableNode.RequiresBlackboardVariablePort(secondPort.Name) &&
                firstPort.GetNode() is not IVariableNode)
            {
                return false;
            }

            if (firstPort.GetNode() is CustomCozyNode firstCustomNode &&
                firstCustomNode.TryGetTypeParameterPortDefinition(
                    firstPort.Name,
                    out _,
                    out _))
            {
                return firstCustomNode.CanConnectTypeParameterPort(firstPort, secondPort);
            }

            if (secondPort.GetNode() is CustomCozyNode secondCustomNode &&
                secondCustomNode.TryGetTypeParameterPortDefinition(
                    secondPort.Name,
                    out _,
                    out _))
            {
                return secondCustomNode.CanConnectTypeParameterPort(secondPort, firstPort);
            }

            if (base.IsConnectionAllowed(firstPort, secondPort))
                return true;

            return CozyValueConverter.CanConvert(outputPort.DataType, inputPort.DataType);
        }

        /// <summary>
        /// Check if the node should throw a warning if connecting a single output to multiple nodes is illegal.
        /// </summary>
        private void CheckMultipleConnectionWarning(INode node, GraphLogger graphLogger)
        {
            foreach (var port in node.GetOutputPorts())
            {
                if (node is CozyEditorNode cozyEditorNode && cozyEditorNode.IsFlowOutputPort(port.Name))
                {
                    var connectedPorts = new List<IPort>();
                    port.GetConnectedPorts(connectedPorts);

                    if (connectedPorts.Count > 1)
                    {
                        graphLogger.LogWarning(
                            $"Flow output '{port.DisplayName}' has {connectedPorts.Count} connections. " +
                            $"A flow output can only choose one next node at runtime.",
                            node
                        );
                    }
                }
            }
        }

        private static void CheckBlackboardVariableInputs(INode node, GraphLogger graphLogger)
        {
            if (node is not CustomCozyNode customNode)
                return;

            foreach (IPort inputPort in node.GetInputPorts())
            {
                if (!customNode.RequiresBlackboardVariablePort(inputPort.Name))
                    continue;

                IPort connectedPort = inputPort.FirstConnectedPort;
                if (connectedPort != null && connectedPort.GetNode() is IVariableNode)
                    continue;

                graphLogger.LogError(
                    $"{customNode.Title} needs a Blackboard variable connected to " +
                    $"'{inputPort.DisplayName}'.",
                    node);
            }
        }

    }
}
