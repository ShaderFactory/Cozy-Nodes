using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Mutable traversal state for one execution flow. A scope owns the shared
    /// graph and Blackboard; each simultaneously running flow owns its own
    /// current node, waits, continuations, and returned subgraph values.
    /// </summary>
    internal sealed class CozyExecutionFrame
    {
        internal CozyGraphScope Scope { get; }
        internal RuntimeCozyGraph Graph => Scope.Graph;
        internal CozyRuntimeVariables Variables => Scope.Variables;
        internal Dictionary<string, RuntimeCozyNode> NodeLookup => Scope.NodeLookup;

        internal Stack<RuntimeCozyNode> ContinuationNodes { get; } =
            new Stack<RuntimeCozyNode>();

        internal Dictionary<string, Dictionary<string, object>>
            SubgraphOutputValuesByNodeID { get; } =
                new Dictionary<string, Dictionary<string, object>>();

        internal RuntimeCozyNode CurrentNode { get; set; }
        internal string EnteredFlowInputName { get; set; }
        internal bool IsWaiting { get; set; }
        internal float RemainingWaitSeconds { get; set; } = -1f;
        internal string TimedWaitOutputPortName { get; set; }

        internal CozyExecutionFrame(CozyGraphScope scope)
        {
            Scope = scope;
        }

        internal void ResetFlowState()
        {
            CurrentNode = null;
            EnteredFlowInputName = null;
            IsWaiting = false;
            RemainingWaitSeconds = -1f;
            TimedWaitOutputPortName = null;
            ContinuationNodes.Clear();
            SubgraphOutputValuesByNodeID.Clear();
        }

        internal void SetSubgraphOutputValues(
            RuntimeCozySubgraphNode subgraphNode,
            Dictionary<string, object> outputValues)
        {
            if (subgraphNode == null || string.IsNullOrWhiteSpace(subgraphNode.NodeID))
                return;

            Dictionary<string, object> copiedValues =
                new Dictionary<string, object>();
            if (outputValues != null)
            {
                foreach (KeyValuePair<string, object> outputValue in outputValues)
                {
                    copiedValues[outputValue.Key] =
                        CozyValueTypeRegistry.CloneForRuntime(outputValue.Value);
                }
            }

            SubgraphOutputValuesByNodeID[subgraphNode.NodeID] = copiedValues;
        }

        internal bool TryGetSubgraphOutputValue(
            RuntimeCozySubgraphNode subgraphNode,
            string outputPortName,
            out object value)
        {
            value = null;
            return subgraphNode != null &&
                   !string.IsNullOrWhiteSpace(subgraphNode.NodeID) &&
                   SubgraphOutputValuesByNodeID.TryGetValue(
                       subgraphNode.NodeID,
                       out Dictionary<string, object> nodeOutputs) &&
                   nodeOutputs.TryGetValue(outputPortName, out value);
        }
    }
}
