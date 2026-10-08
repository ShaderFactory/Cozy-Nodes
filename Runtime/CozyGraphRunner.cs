using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runs one RuntimeCozyGraph. CozyManager handles Unity setup and public API;
    /// this class handles node lookup, flow traversal, waiting and triggers.
    /// </summary>
    public class CozyGraphRunner
    {
        private readonly CozyManager cozyManager;
        private readonly Dictionary<string, RuntimeCozyNode> nodeLookup =
            new Dictionary<string, RuntimeCozyNode>();

        private RuntimeCozyNode currentNode;
        private bool isRunning;
        private bool isWaiting;

        public CozyGraphRunner(CozyManager manager)
        {
            cozyManager = manager;
        }

        /// <summary>
        /// Validates the graph, builds its lookup table and starts at EntryNodeID.
        /// </summary>
        public void Start(RuntimeCozyGraph runtimeGraph)
        {
            if (runtimeGraph == null)
            {
                Debug.LogWarning("Please assign a Graph to the Cozy Manager.");
                return;
            }

            BuildNodeLookup(runtimeGraph);

            if (string.IsNullOrEmpty(runtimeGraph.EntryNodeID))
            {
                Debug.Log("Graph has no entry node.");
                return;
            }

            StartGraph(runtimeGraph.EntryNodeID);
        }

        /// <summary>
        /// Starts execution at a runtime node. The loop is iterative so long graphs
        /// do not grow the C# call stack one node at a time.
        /// </summary>
        public void StartGraph(string entryNodeID)
        {
            if (!nodeLookup.TryGetValue(entryNodeID, out currentNode))
            {
                Debug.LogWarning("Entry node was not found: " + entryNodeID);
                return;
            }

            isRunning = true;
            isWaiting = false;
            RunUntilPauseOrEnd();
        }

        /// <summary>
        /// Sends a trigger only to the flow node currently waiting in this runner.
        /// A non-matching trigger is ignored and leaves the graph paused.
        /// </summary>
        public void Trigger(string triggerName)
        {
            if (!isRunning || !isWaiting || currentNode == null)
                return;

            if (currentNode is not ICozyTriggerListener triggerListener)
                return;

            if (!triggerListener.TryReceiveTrigger(triggerName, cozyManager))
                return;

            isWaiting = false;
            RunUntilPauseOrEnd();
        }

        private void BuildNodeLookup(RuntimeCozyGraph runtimeGraph)
        {
            nodeLookup.Clear();

            foreach (RuntimeCozyNode node in runtimeGraph.AllNodes)
                nodeLookup[node.NodeID] = node;
        }

        private void RunUntilPauseOrEnd()
        {
            while (isRunning && currentNode != null)
            {
                CozyNodeExecutionResult result = currentNode.Run(cozyManager);
                if (result == null)
                {
                    Debug.LogError($"Node '{currentNode.NodeType}' returned no execution result.");
                    StopGraph();
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.Waiting)
                {
                    isWaiting = true;
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.Finished)
                {
                    StopGraph();
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.Failed)
                {
                    Debug.LogError(result.ErrorMessage);
                    StopGraph();
                    return;
                }

                if (string.IsNullOrEmpty(result.OutputPortName))
                {
                    Debug.LogError($"Node '{currentNode.NodeType}' continued without choosing a flow output.");
                    StopGraph();
                    return;
                }

                if (!currentNode.TryGetFlowOutput(result.OutputPortName, out string nextNodeID))
                {
                    Debug.LogWarning($"Node '{currentNode.NodeType}' has no connection on flow output '{result.OutputPortName}'.");
                    StopGraph();
                    return;
                }

                if (!nodeLookup.TryGetValue(nextNodeID, out currentNode))
                {
                    Debug.LogError($"Flow output '{result.OutputPortName}' points to a missing node.");
                    StopGraph();
                }
            }
        }

        private void StopGraph()
        {
            isRunning = false;
            isWaiting = false;
            currentNode = null;
            Debug.Log("Graph ended.");
        }
    }
}
