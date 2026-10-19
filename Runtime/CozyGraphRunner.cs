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
        private CozyExecutionFrame rootFrame;
        private CozyExecutionFrame activeFrame;
        private readonly Stack<CozyExecutionFrame> suspendedFrames =
            new Stack<CozyExecutionFrame>();
        private bool isRunning;

        public CozyGraphRunner(CozyManager manager)
        {
            cozyManager = manager;
        }

        /// <summary>
        /// Validates the graph and builds its lookup table. Starting an entry is
        /// deliberately separate so CozyManager can choose the lifecycle entry
        /// and game code can later run a named On Event entry.
        /// </summary>
        public bool Initialize(RuntimeCozyGraph graph)
        {
            StopGraph();

            if (graph == null)
            {
                Debug.LogWarning("Please assign a Graph to the Cozy Manager.");
                return false;
            }

            rootFrame = new CozyExecutionFrame(graph, cozyManager.RuntimeVariables);
            ActivateFrame(rootFrame);
            return true;
        }

        /// <summary>
        /// Runs the graph's automatic On Start entry, when it has one.
        /// A graph that only contains On Event entries is valid and stays idle.
        /// </summary>
        public bool RunOnStart()
        {
            return RunEntry(CozyGraphEntryKind.OnStart, null, false);
        }

        /// <summary>
        /// Starts one named On Event entry. This is an external graph start,
        /// unlike a Wait For Event node, which resumes an already-running flow.
        /// </summary>
        public bool RunCustomEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("On Event needs a name.");
                return false;
            }

            return RunEntry(CozyGraphEntryKind.CustomEvent, eventName, true);
        }

        private bool RunEntry(
            CozyGraphEntryKind kind,
            string entryName,
            bool reportMissingEntry)
        {
            if (rootFrame?.Graph == null)
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

            if (!rootFrame.Graph.TryGetEntry(kind, entryName, out RuntimeCozyGraphEntry entry))
            {
                if (reportMissingEntry)
                    Debug.LogWarning($"On Event '{entryName}' was not found in this graph.");

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
            if (rootFrame == null ||
                !rootFrame.NodeLookup.TryGetValue(entryNodeID, out RuntimeCozyNode entryNode))
            {
                Debug.LogWarning("Entry node was not found: " + entryNodeID);
                return false;
            }

            isRunning = true;
            suspendedFrames.Clear();
            ActivateFrame(rootFrame);
            rootFrame.ResetFlowState();
            rootFrame.CurrentNode = entryNode;
            rootFrame.EnteredFlowInputName = string.IsNullOrWhiteSpace(entryInputPortName)
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
            if (!isRunning || activeFrame == null ||
                !activeFrame.IsWaiting || activeFrame.CurrentNode == null)
                return;

            if (activeFrame.CurrentNode is not ICozyTriggerListener triggerListener)
                return;

            if (!triggerListener.TryReceiveTrigger(triggerName, cozyManager))
                return;

            activeFrame.IsWaiting = false;
            RunUntilPauseOrEnd();
        }

        /// <summary>
        /// Advances a timed graph pause using scaled game time. CozyManager calls
        /// this from Update; a normal trigger wait is intentionally unaffected.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!isRunning || activeFrame == null ||
                !activeFrame.IsWaiting || activeFrame.RemainingWaitSeconds < 0f)
                return;

            activeFrame.RemainingWaitSeconds -= Math.Max(0f, deltaTime);
            if (activeFrame.RemainingWaitSeconds > 0f)
                return;

            string outputPortName = activeFrame.TimedWaitOutputPortName;
            activeFrame.RemainingWaitSeconds = -1f;
            activeFrame.TimedWaitOutputPortName = null;
            activeFrame.IsWaiting = false;

            if (string.IsNullOrEmpty(outputPortName))
            {
                Debug.LogError("Timed wait did not choose a flow output.");
                StopGraph();
                return;
            }

            if (TryAdvanceFlow(CozyNodeExecutionResult.ContinueWith(outputPortName)))
                RunUntilPauseOrEnd();
        }

        private void RunUntilPauseOrEnd()
        {
            while (isRunning && activeFrame?.CurrentNode != null)
            {
                FlowNodeExecuting?.Invoke(activeFrame.CurrentNode);
                CozyNodeExecutionResult result = activeFrame.CurrentNode.Run(
                    cozyManager,
                    activeFrame.EnteredFlowInputName);
                if (result == null)
                {
                    Debug.LogError($"Node '{activeFrame.CurrentNode.NodeType}' returned no execution result.");
                    StopGraph();
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.EnterSubgraph)
                {
                    if (!TryEnterSubgraph(result))
                        return;

                    continue;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.ReturnFromSubgraph)
                {
                    if (!TryReturnFromSubgraph(result))
                        return;

                    continue;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.Waiting)
                {
                    if (result.IsTimedWait)
                    {
                        if (string.IsNullOrEmpty(result.OutputPortName))
                        {
                            Debug.LogError(
                                $"Node '{activeFrame.CurrentNode.NodeType}' started a timed wait without choosing a flow output.");
                            StopGraph();
                            return;
                        }

                        activeFrame.RemainingWaitSeconds = result.WaitDurationSeconds;
                        activeFrame.TimedWaitOutputPortName = result.OutputPortName;
                    }

                    activeFrame.IsWaiting = true;
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

            if (exitsScope && !TryExitScope(activeFrame.CurrentNode))
            {
                Debug.LogError(
                    $"Node '{activeFrame.CurrentNode.NodeType}' tried to exit a flow scope that is not active.");
                StopGraph();
                return false;
            }

            if (string.IsNullOrEmpty(result.OutputPortName))
            {
                Debug.LogError($"Node '{activeFrame.CurrentNode.NodeType}' continued without choosing a flow output.");
                StopGraph();
                return false;
            }

            if (!activeFrame.CurrentNode.TryGetFlowOutput(
                    result.OutputPortName,
                    out RuntimeFlowConnection flowConnection))
            {
                if (isCall)
                {
                    // Calling an unconnected body is an immediate return to
                    // the caller, which can advance to its next iteration.
                    activeFrame.EnteredFlowInputName = null;
                    return true;
                }

                if (TryResumeContinuation())
                    return true;

                if (suspendedFrames.Count > 0)
                {
                    Debug.LogError(
                        $"Child graph '{activeFrame.Graph.name}' ended without reaching a flow output gateway.");
                    StopGraph();
                    return false;
                }

                // An unconnected flow output is a valid end to the root graph.
                StopGraph();
                return false;
            }

            RuntimeCozyNode callingNode = activeFrame.CurrentNode;
            if (!activeFrame.NodeLookup.TryGetValue(flowConnection.NextNodeID, out RuntimeCozyNode nextNode))
            {
                Debug.LogError($"Flow output '{result.OutputPortName}' points to a missing node.");
                StopGraph();
                return false;
            }

            if (isCall)
                activeFrame.ContinuationNodes.Push(callingNode);

            activeFrame.CurrentNode = nextNode;
            activeFrame.EnteredFlowInputName = string.IsNullOrWhiteSpace(flowConnection.NextInputPortName)
                ? "in"
                : flowConnection.NextInputPortName;
            return true;
        }

        private bool TryResumeContinuation()
        {
            if (activeFrame.ContinuationNodes.Count == 0)
                return false;

            activeFrame.CurrentNode = activeFrame.ContinuationNodes.Pop();
            activeFrame.EnteredFlowInputName = null;
            return true;
        }

        /// <summary>
        /// Suspends the caller at its subgraph node, then gives the child its own
        /// node lookup, continuation stack and Blackboard value scope. The caller
        /// remains untouched until a child exit gateway chooses its return path.
        /// </summary>
        private bool TryEnterSubgraph(CozyNodeExecutionResult result)
        {
            if (result.Subgraph == null)
            {
                Debug.LogError("A subgraph call did not provide an imported child graph.");
                StopGraph();
                return false;
            }

            if (string.IsNullOrEmpty(result.TargetNodeID))
            {
                Debug.LogError(
                    $"Subgraph '{result.Subgraph.name}' has a flow input without a target node.");
                StopGraph();
                return false;
            }

            CozyExecutionFrame childFrame = new CozyExecutionFrame(
                result.Subgraph,
                new CozyRuntimeVariables(result.Subgraph.VariableDefinitions));

            string dataInputError = null;
            if (activeFrame.CurrentNode is not RuntimeCozySubgraphNode subgraphNode ||
                !subgraphNode.TryPopulateChildVariables(
                    cozyManager,
                    childFrame.Variables,
                    out dataInputError))
            {
                Debug.LogError(
                    dataInputError ??
                    "A subgraph call did not originate from a RuntimeCozySubgraphNode.");
                StopGraph();
                return false;
            }

            if (!childFrame.NodeLookup.TryGetValue(
                    result.TargetNodeID,
                    out RuntimeCozyNode entryNode))
            {
                Debug.LogError(
                    $"Subgraph '{result.Subgraph.name}' points to a missing entry node.");
                StopGraph();
                return false;
            }

            suspendedFrames.Push(activeFrame);
            ActivateFrame(childFrame);
            activeFrame.CurrentNode = entryNode;
            activeFrame.EnteredFlowInputName = string.IsNullOrWhiteSpace(
                result.TargetInputPortName)
                ? "in"
                : result.TargetInputPortName;
            return true;
        }

        /// <summary>
        /// Restores the suspended caller and follows the flow output named by the
        /// child gateway. This uses the same flow-resolution path as ordinary
        /// nodes, so loops and unconnected outputs keep their normal behavior.
        /// </summary>
        private bool TryReturnFromSubgraph(CozyNodeExecutionResult result)
        {
            if (suspendedFrames.Count == 0)
            {
                Debug.LogError("A subgraph flow output was reached without a calling graph.");
                StopGraph();
                return false;
            }

            if (string.IsNullOrEmpty(result.OutputPortName))
            {
                Debug.LogError("A subgraph flow output has no name.");
                StopGraph();
                return false;
            }

            CozyExecutionFrame childFrame = activeFrame;
            CozyExecutionFrame parentFrame = suspendedFrames.Peek();
            if (parentFrame.CurrentNode is not RuntimeCozySubgraphNode subgraphNode)
            {
                Debug.LogError("A subgraph returned to a caller that is not a subgraph node.");
                StopGraph();
                return false;
            }

            if (!subgraphNode.TryCollectChildOutputValues(
                    cozyManager,
                    childFrame.Variables,
                    out Dictionary<string, object> outputValues,
                    out string dataOutputError))
            {
                Debug.LogError(dataOutputError);
                StopGraph();
                return false;
            }

            parentFrame.SetSubgraphOutputValues(subgraphNode, outputValues);

            CancelFrame(childFrame);
            ActivateFrame(suspendedFrames.Pop());
            return TryAdvanceFlow(CozyNodeExecutionResult.ContinueWith(result.OutputPortName));
        }

        internal bool TryGetSubgraphOutputValue(
            RuntimeCozySubgraphNode subgraphNode,
            string outputPortName,
            out object value)
        {
            if (activeFrame == null)
            {
                value = null;
                return false;
            }

            return activeFrame.TryGetSubgraphOutputValue(
                subgraphNode,
                outputPortName,
                out value);
        }

        private bool TryExitScope(RuntimeCozyNode scopeNode)
        {
            bool isActiveScope = false;
            foreach (RuntimeCozyNode continuationNode in activeFrame.ContinuationNodes)
            {
                if (continuationNode == scopeNode)
                {
                    isActiveScope = true;
                    break;
                }
            }

            if (!isActiveScope)
                return false;

            while (activeFrame.ContinuationNodes.Count > 0)
            {
                RuntimeCozyNode cancelledNode = activeFrame.ContinuationNodes.Pop();
                cancelledNode?.CancelFlowScope(cozyManager);

                if (cancelledNode == scopeNode)
                    return true;
            }

            return false;
        }

        private void StopGraph()
        {
            isRunning = false;
            if (activeFrame == null)
                return;

            CancelFrame(activeFrame);

            while (suspendedFrames.Count > 0)
                CancelFrame(suspendedFrames.Pop());

            if (rootFrame != null)
                ActivateFrame(rootFrame);
        }

        private void ActivateFrame(CozyExecutionFrame frame)
        {
            activeFrame = frame;
            cozyManager.SetRuntimeVariables(frame?.Variables);
        }

        private void CancelFrame(CozyExecutionFrame frame)
        {
            if (frame == null)
                return;

            frame.CurrentNode?.CancelFlowScope(cozyManager);

            while (frame.ContinuationNodes.Count > 0)
                frame.ContinuationNodes.Pop()?.CancelFlowScope(cozyManager);

            frame.ResetFlowState();
        }
    }
}
