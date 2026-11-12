using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared mutable data for one active invocation of a runtime graph. Multiple
    /// flows can run inside this scope and therefore see the same Blackboard.
    /// A subgraph call creates a child scope with its own variables.
    /// </summary>
    internal sealed class CozyGraphScope
    {
        internal RuntimeCozyGraph Graph { get; }
        internal CozyRuntimeVariables Variables { get; }
        internal Dictionary<string, RuntimeCozyNode> NodeLookup { get; } =
            new Dictionary<string, RuntimeCozyNode>();

        internal CozyGraphScope ParentScope { get; }
        internal CozyExecutionFlow CallingFlow { get; }
        internal CozyExecutionFrame CallingFrame { get; }
        internal RuntimeCozySubgraphNode CallingNode { get; }
        internal CozyExecutionFlow PrimaryFlow { get; set; }

        internal bool IsRoot => ParentScope == null;

        internal CozyGraphScope(
            RuntimeCozyGraph graph,
            CozyRuntimeVariables variables,
            CozyGraphScope parentScope = null,
            CozyExecutionFlow callingFlow = null,
            CozyExecutionFrame callingFrame = null,
            RuntimeCozySubgraphNode callingNode = null)
        {
            Graph = graph;
            Variables = variables;
            ParentScope = parentScope;
            CallingFlow = callingFlow;
            CallingFrame = callingFrame;
            CallingNode = callingNode;

            if (graph?.AllNodes == null)
                return;

            foreach (RuntimeCozyNode node in graph.AllNodes)
            {
                if (node != null && !string.IsNullOrWhiteSpace(node.NodeID))
                    NodeLookup[node.NodeID] = node;
            }
        }
    }
}
