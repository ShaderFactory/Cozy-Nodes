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
        }
    }
}
