using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Owns the mutable state for one active graph execution.
    ///
    /// A Cozy graph asset is an immutable imported template at runtime. Values
    /// that change while it runs - the current node, its variable scope, waits,
    /// and call continuations - live here instead. The root runner currently
    /// creates one frame; nested graph calls can later create their own frames
    /// without sharing any of that state.
    /// </summary>
    internal sealed class CozyExecutionFrame
    {
        internal RuntimeCozyGraph Graph { get; }
        internal CozyRuntimeVariables Variables { get; }
        internal Dictionary<string, RuntimeCozyNode> NodeLookup { get; } =
            new Dictionary<string, RuntimeCozyNode>();
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

        internal CozyExecutionFrame(RuntimeCozyGraph graph, CozyRuntimeVariables variables)
        {
            Graph = graph;
            Variables = variables;

            if (graph?.AllNodes == null)
                return;

            foreach (RuntimeCozyNode node in graph.AllNodes)
            {
                if (node != null && !string.IsNullOrWhiteSpace(node.NodeID))
                    NodeLookup[node.NodeID] = node;
            }
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

        /// <summary>
        /// Keeps returned values on the calling execution frame rather than on
        /// the imported node asset. A single asset can safely run in multiple
        /// CozyManagers and can be called more than once in one execution.
        /// </summary>
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
