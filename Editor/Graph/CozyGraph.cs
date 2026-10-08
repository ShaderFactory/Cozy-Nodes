using UnityEngine;
using UnityEditor;
using Unity.GraphToolkit.Editor;
using System;
using System.Collections.Generic;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    [Graph(AssetExtension)]
    public class CozyGraph : Graph
    {
        public const string AssetExtension = "cozygraph";

        [MenuItem("Assets/Create/Shader Factory/Cozy Graph Toolkit/Graph", false)]
        private static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<CozyGraph>();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            Debug.Log("Graph Changed");
            base.OnGraphChanged(graphLogger);

            // Validate all nodes
            foreach (var node in GetNodes())
            {
                CheckMultipleConnectionWarning(node, graphLogger);
                CheckSetVariableError(node, graphLogger);
            }
        }

        /// <summary>
        /// Keeps ports strongly typed while allowing only Cozy's explicitly safe
        /// conversions, such as integer to text for a Print Node. Graph Toolkit still
        /// owns all normal same-type connection rules.
        /// </summary>
        public override bool IsConnectionAllowed(IPort firstPort, IPort secondPort)
        {
            if (base.IsConnectionAllowed(firstPort, secondPort))
                return true;

            if (firstPort == null || secondPort == null)
                return false;

            IPort outputPort = firstPort.Direction == PortDirection.Output ? firstPort : secondPort;
            IPort inputPort = firstPort.Direction == PortDirection.Input ? firstPort : secondPort;

            if (outputPort.Direction != PortDirection.Output || inputPort.Direction != PortDirection.Input)
                return false;

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

        /// <summary>
        /// Set Variable has generic ports so it can support every Blackboard type.
        /// Validate the two connections here, where Graph Toolkit can show the
        /// problem directly on the node instead of waiting for Play Mode.
        /// </summary>
        private void CheckSetVariableError(INode node, GraphLogger graphLogger)
        {
            if (node is not SetVariableNode setVariableNode)
                return;

            IPort variableInput = setVariableNode.GetInputPortByName("Variable");
            IPort valueInput = setVariableNode.GetInputPortByName("Value");
            IPort variableSource = variableInput?.FirstConnectedPort;
            IPort valueSource = valueInput?.FirstConnectedPort;

            if (variableSource == null || variableSource.GetNode() is not IVariableNode variableNode)
            {
                graphLogger.LogError(
                    "Set Variable needs a Blackboard variable connected to Variable.",
                    setVariableNode);
                return;
            }

            if (valueSource == null || variableNode.Variable == null)
                return;

            if (valueSource.DataType == null || variableNode.Variable.DataType == null)
            {
                graphLogger.LogError(
                    "Set Variable could not determine the connected value type.",
                    setVariableNode);
                return;
            }

            if (!CozyValueConverter.CanConvert(valueSource.DataType, variableNode.Variable.DataType))
            {
                graphLogger.LogError(
                    $"Set Variable cannot convert {valueSource.DataType.Name} to {variableNode.Variable.DataType.Name} for '{variableNode.Variable.Name}'.",
                    setVariableNode);
            }
        }
    }
}
