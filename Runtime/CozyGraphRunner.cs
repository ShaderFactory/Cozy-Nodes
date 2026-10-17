using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Runs one RuntimeCozyGraph. CozyManager handles Unity setup and public API;
    /// this class handles node lookup, flow traversal, waiting and triggers.
    /// </summary>
    public class CozyGraphRunner
    {
        /// <summary>
        /// Raised immediately before a flow node executes. The runner has no
        /// Editor dependency; consumers can use the stable runtime node ID for
        /// diagnostics or Editor-only execution visualization.
        /// </summary>
        public event Action<RuntimeCozyNode> FlowNodeExecuting;

        private readonly CozyManager cozyManager;
        private readonly Dictionary<string, RuntimeCozyNode> nodeLookup =
            new Dictionary<string, RuntimeCozyNode>();
        private readonly Stack<FlowContinuation> continuationNodes =
            new Stack<FlowContinuation>();

        private RuntimeCozyGraph runtimeGraph;
        private RuntimeCozyNode currentNode;
        private string enteredFlowInputName;
        private bool isRunning;
        private bool isWaiting;
        private float remainingWaitSeconds = -1f;
        private string timedWaitOutputPortName;

        private readonly struct FlowContinuation
        {
            public RuntimeCozyNode Node { get; }

            public FlowContinuation(RuntimeCozyNode node)
            {
                Node = node;
            }
        }

        public CozyGraphRunner(CozyManager manager)
        {
            cozyManager = manager;
        }

        /// <summary>
        /// Validates the graph and builds its lookup table. Starting an entry is
        /// deliberately separate so CozyManager can choose the lifecycle entry
        /// and game code can later run a named Custom Event entry.
        /// </summary>
        public bool Initialize(RuntimeCozyGraph graph)
        {
            StopGraph();
            runtimeGraph = graph;

            if (runtimeGraph == null)
            {
                Debug.LogWarning("Please assign a Graph to the Cozy Manager.");
                return false;
            }

            BuildNodeLookup(runtimeGraph);
            return true;
        }

        /// <summary>
        /// Runs the graph's automatic On Start entry, when it has one.
        /// A graph that only contains Custom Events is valid and stays idle.
        /// </summary>
        public bool RunOnStart()
        {
            return RunEntry(CozyGraphEntryKind.OnStart, null, false);
        }

        /// <summary>
        /// Starts one named Custom Event entry. This is an external graph start,
        /// unlike a Wait for Trigger node, which resumes an already-running flow.
        /// </summary>
        public bool RunCustomEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("Custom Event needs a name.");
                return false;
            }

            return RunEntry(CozyGraphEntryKind.CustomEvent, eventName, true);
        }

        private bool RunEntry(
            CozyGraphEntryKind kind,
            string entryName,
            bool reportMissingEntry)
        {
            if (runtimeGraph == null)
            {
                Debug.LogWarning("Cozy Graph has not been initialized.");
                return false;
            }

            if (isRunning)
            {
                Debug.LogWarning(
                    "Cozy Graph is already running. A CozyManager currently runs one " +
                    "flow at a time, so the requested entry was not started.");
                return false;
            }

            if (!runtimeGraph.TryGetEntry(kind, entryName, out RuntimeCozyGraphEntry entry))
            {
                if (reportMissingEntry)
                    Debug.LogWarning($"Custom Event '{entryName}' was not found in this graph.");

                return false;
            }

            return StartGraph(entry.TargetNodeID, entry.TargetInputPortName);
        }

        /// <summary>
        /// Starts execution at a runtime node. The loop is iterative so long graphs
        /// do not grow the C# call stack one node at a time.
        /// </summary>
        private bool StartGraph(string entryNodeID, string entryInputPortName)
        {
            if (!nodeLookup.TryGetValue(entryNodeID, out currentNode))
            {
                Debug.LogWarning("Entry node was not found: " + entryNodeID);
                return false;
            }

            isRunning = true;
            isWaiting = false;
            remainingWaitSeconds = -1f;
            timedWaitOutputPortName = null;
            continuationNodes.Clear();
            enteredFlowInputName = string.IsNullOrWhiteSpace(entryInputPortName)
                ? "in"
                : entryInputPortName;
            RunUntilPauseOrEnd();
            return true;
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

        /// <summary>
        /// Advances a timed graph pause using scaled game time. CozyManager calls
        /// this from Update; a normal trigger wait is intentionally unaffected.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!isRunning || !isWaiting || remainingWaitSeconds < 0f)
                return;

            remainingWaitSeconds -= Math.Max(0f, deltaTime);
            if (remainingWaitSeconds > 0f)
                return;

            string outputPortName = timedWaitOutputPortName;
            remainingWaitSeconds = -1f;
            timedWaitOutputPortName = null;
            isWaiting = false;

            if (string.IsNullOrEmpty(outputPortName))
            {
                Debug.LogError("Timed wait did not choose a flow output.");
                StopGraph();
                return;
            }

            if (TryAdvanceFlow(CozyNodeExecutionResult.ContinueWith(outputPortName)))
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
                FlowNodeExecuting?.Invoke(currentNode);
                CozyNodeExecutionResult result = currentNode.Run(
                    cozyManager,
                    enteredFlowInputName);
                if (result == null)
                {
                    Debug.LogError($"Node '{currentNode.NodeType}' returned no execution result.");
                    StopGraph();
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.Waiting)
                {
                    if (result.IsTimedWait)
                    {
                        if (string.IsNullOrEmpty(result.OutputPortName))
                        {
                            Debug.LogError(
                                $"Node '{currentNode.NodeType}' started a timed wait without choosing a flow output.");
                            StopGraph();
                            return;
                        }

                        remainingWaitSeconds = result.WaitDurationSeconds;
                        timedWaitOutputPortName = result.OutputPortName;
                    }

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

                if (!TryAdvanceFlow(result))
                    return;
            }
        }

        /// <summary>
        /// Follows one flow result from the current node. Both immediate results
        /// and a completed timed wait use this path, so their continuation and
        /// loop-scope behavior cannot drift apart.
        /// </summary>
        private bool TryAdvanceFlow(CozyNodeExecutionResult result)
        {
            bool isCall = result.ExecutionState == CozyNodeExecutionResult.State.Call;
            bool exitsScope = result.ExecutionState == CozyNodeExecutionResult.State.ExitScope;

            if (exitsScope && !TryExitScope(currentNode))
            {
                Debug.LogError(
                    $"Node '{currentNode.NodeType}' tried to exit a flow scope that is not active.");
                StopGraph();
                return false;
            }

            if (string.IsNullOrEmpty(result.OutputPortName))
            {
                Debug.LogError($"Node '{currentNode.NodeType}' continued without choosing a flow output.");
                StopGraph();
                return false;
            }

            if (!currentNode.TryGetFlowOutput(
                    result.OutputPortName,
                    out RuntimeFlowConnection flowConnection))
            {
                if (isCall)
                {
                    // Calling an unconnected body is an immediate return to
                    // the caller, which can advance to its next iteration.
                    enteredFlowInputName = null;
                    return true;
                }

                if (TryResumeContinuation())
                    return true;

                // An unconnected flow output is a valid end to a graph.
                StopGraph();
                return false;
            }

            RuntimeCozyNode callingNode = currentNode;
            if (!nodeLookup.TryGetValue(flowConnection.NextNodeID, out RuntimeCozyNode nextNode))
            {
                Debug.LogError($"Flow output '{result.OutputPortName}' points to a missing node.");
                StopGraph();
                return false;
            }

            if (isCall)
                continuationNodes.Push(new FlowContinuation(callingNode));

            currentNode = nextNode;
            enteredFlowInputName = string.IsNullOrWhiteSpace(flowConnection.NextInputPortName)
                ? "in"
                : flowConnection.NextInputPortName;
            return true;
        }

        private bool TryResumeContinuation()
        {
            if (continuationNodes.Count == 0)
                return false;

            currentNode = continuationNodes.Pop().Node;
            enteredFlowInputName = null;
            return true;
        }

        private bool TryExitScope(RuntimeCozyNode scopeNode)
        {
            bool isActiveScope = false;
            foreach (FlowContinuation continuation in continuationNodes)
            {
                if (continuation.Node == scopeNode)
                {
                    isActiveScope = true;
                    break;
                }
            }

            if (!isActiveScope)
                return false;

            while (continuationNodes.Count > 0)
            {
                RuntimeCozyNode cancelledNode = continuationNodes.Pop().Node;
                cancelledNode?.CancelFlowScope(cozyManager);

                if (cancelledNode == scopeNode)
                    return true;
            }

            return false;
        }

        private void StopGraph()
        {
            isRunning = false;
            isWaiting = false;
            remainingWaitSeconds = -1f;
            timedWaitOutputPortName = null;
            currentNode?.CancelFlowScope(cozyManager);
            currentNode = null;
            enteredFlowInputName = null;

            while (continuationNodes.Count > 0)
                continuationNodes.Pop().Node?.CancelFlowScope(cozyManager);
        }
    }
}
