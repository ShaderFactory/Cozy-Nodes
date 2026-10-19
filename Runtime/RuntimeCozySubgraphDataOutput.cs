using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Connects a child graph value source to one output on its parent-facing
    /// local-subgraph node. Graph Toolkit represents a data output as a child
    /// variable node with an incoming wire, so the source must be evaluated as
    /// the child returns rather than read as the variable's default value.
    /// </summary>
    [Serializable]
    public sealed class RuntimeCozySubgraphDataOutput
    {
        // Graph Toolkit names the parent port with the child variable's stable ID.
        public string ParentPortName;
        public string ChildVariableID;
        public string ValueTypeName;

        public bool HasConnectedSource;
        public CozyRuntimePort Source = new CozyRuntimePort();

        /// <summary>
        /// Resolves the wire that feeds the child output endpoint. An unconnected
        /// endpoint intentionally falls back to the child Blackboard value so a
        /// graph can return its declared default value.
        /// </summary>
        public bool TryResolveValue(
            RuntimeCozyGraph childGraph,
            CozyManager cozyManager,
            CozyRuntimeVariables childVariables,
            out object value,
            out string errorMessage)
        {
            value = null;
            errorMessage = null;

            if (!HasConnectedSource)
            {
                if (childVariables != null &&
                    childVariables.TryGetValue(ChildVariableID, out value))
                {
                    return true;
                }

                errorMessage = "The child Blackboard value is unavailable.";
                return false;
            }

            if (Source == null)
            {
                errorMessage = "The child output source was not imported.";
                return false;
            }

            if (Source.type == CozyRuntimePort.PortType.Value)
            {
                value = Source.GetValue();
                return true;
            }

            if (Source.type == CozyRuntimePort.PortType.Variable)
            {
                if (Source.variableReference != null &&
                    childVariables != null &&
                    childVariables.TryGetValue(
                        Source.variableReference.variableID,
                        out value))
                {
                    return true;
                }

                errorMessage = "The child output source references an unavailable Blackboard variable.";
                return false;
            }

            if (Source.type != CozyRuntimePort.PortType.Port ||
                Source.portReference == null ||
                childGraph?.AllNodes == null)
            {
                errorMessage = "The child output source is invalid.";
                return false;
            }

            foreach (RuntimeCozyNode node in childGraph.AllNodes)
            {
                if (node == null || node.NodeID != Source.portReference.nodeID)
                    continue;

                CozyRuntimePort sourcePort = node.GetPortByName(Source.portReference.portName);
                if (sourcePort == null)
                {
                    errorMessage = "The child output source port is missing.";
                    return false;
                }

                value = node.GetValue(sourcePort, cozyManager);
                return true;
            }

            errorMessage = "The child output source node is missing.";
            return false;
        }
    }
}
