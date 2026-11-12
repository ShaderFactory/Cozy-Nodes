using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Schedules one primary flow and zero or more event flows for a graph
    /// scope. The primary flow owns normal graph progression; On Event entries
    /// are independent reactions that share that scope's Blackboard.
    /// </summary>
    internal sealed class CozyGraphRunner
    {
        internal event Action<RuntimeCozyNode> FlowNodeExecuting;

        private readonly CozyGraphSession session;
        private readonly List<CozyGraphScope> activeScopes =
            new List<CozyGraphScope>();
        private readonly List<CozyExecutionFlow> activeFlows =
            new List<CozyExecutionFlow>();

        private CozyGraphScope rootScope;
        private bool isCompleted;

        internal CozyGraphRunner(CozyGraphSession session)
        {
            this.session = session;
        }

        /// <summary>
        /// Creates the root Blackboard scope. Starting an entry is separate so
        /// callers can decide whether to run On Graph Start, On Update, or a
        /// named On Event entry.
        /// </summary>
        internal bool Initialize(RuntimeCozyGraph graph)
        {
            StopGraph();
            activeScopes.Clear();

            if (graph == null)
            {
                Debug.LogWarning("Cannot start a CozyGraphSession without a graph.");
                return false;
            }

            rootScope = new CozyGraphScope(
                graph,
                new CozyRuntimeVariables(graph.VariableDefinitions));
            activeScopes.Add(rootScope);
            isCompleted = false;
            RestoreDefaultScope();
            return true;
        }

        /// <summary>
        /// Starts the root graph's one primary flow. That flow is the only root
        /// path that represents normal graph progression.
        /// </summary>
        internal bool RunOnStart()
        {
            isCompleted = false;
            bool started = StartPrimaryEntry(rootScope, CozyGraphEntryKind.OnStart);
            RestoreDefaultScope();
            return started;
        }

        internal bool RunOnUpdate()
        {
            if (isCompleted)
                return false;

            bool started = StartAuxiliaryEntry(
                rootScope,
                CozyGraphEntryKind.OnUpdate,
                null,
                false);
            RestoreDefaultScope();
            return started;
        }

        internal bool RunOnLateUpdate()
        {
            if (isCompleted)
                return false;

            bool started = StartAuxiliaryEntry(
                rootScope,
                CozyGraphEntryKind.OnLateUpdate,
                null,
                false);
            RestoreDefaultScope();
            return started;
        }

        /// <summary>
        /// Starts a named root On Event entry. This is useful when code already
        /// owns a session; global signals use <see cref="DispatchEvent"/> so
        /// active child scopes can react too.
        /// </summary>
        internal bool RunCustomEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("On Event needs a name.");
                return false;
            }

            isCompleted = false;
            bool started = StartAuxiliaryEntry(
                rootScope,
                CozyGraphEntryKind.CustomEvent,
                eventName,
                true);
            RestoreDefaultScope();
            return started;
        }

        /// <summary>
        /// Delivers a named event to this session. Matching Wait For Event nodes
        /// resume, and matching On Event entries start in every live graph scope.
        /// The same On Event entry never starts twice while its prior invocation
        /// is active or waiting.
        /// </summary>
        internal void DispatchEvent(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("A Cozy graph event needs a name.");
                return;
            }

            if (isCompleted)
                return;

            TriggerInternal(eventName);

            List<CozyGraphScope> scopes =
                new List<CozyGraphScope>(activeScopes);
            foreach (CozyGraphScope scope in scopes)
            {
                if (!IsScopeActive(scope))
                    continue;

                StartAuxiliaryEntry(
                    scope,
                    CozyGraphEntryKind.CustomEvent,
                    eventName,
                    false);
            }

            RestoreDefaultScope();
        }

        /// <summary>
        /// Resumes every matching Wait For Event node in this session. Unlike
        /// <see cref="DispatchEvent"/>, this does not start On Event entries.
        /// </summary>
        internal void Trigger(string triggerName)
        {
            if (isCompleted)
                return;

            TriggerInternal(triggerName);
            RestoreDefaultScope();
        }

        private void TriggerInternal(string triggerName)
        {
            if (string.IsNullOrWhiteSpace(triggerName))
                return;

            List<CozyExecutionFlow> flows =
                new List<CozyExecutionFlow>(activeFlows);
            foreach (CozyExecutionFlow flow in flows)
            {
                if (!IsFlowActive(flow) || flow.IsSuspended)
                    continue;

                CozyExecutionFrame frame = flow.ActiveFrame;
                if (!frame.IsWaiting || frame.CurrentNode == null ||
                    frame.CurrentNode is not ICozyTriggerListener triggerListener)
                    continue;

                ActivateFlow(flow);
                if (!triggerListener.TryReceiveTrigger(triggerName, session))
                    continue;

                frame.IsWaiting = false;
                RunFlow(flow);
            }
        }

        /// <summary>
        /// Advances all timed waits. Each flow owns its own timer, so a delayed
        /// event handler never pauses the primary flow or another event handler.
        /// </summary>
        internal void Update(float deltaTime)
        {
            List<CozyExecutionFlow> flows =
                new List<CozyExecutionFlow>(activeFlows);
            foreach (CozyExecutionFlow flow in flows)
            {
                if (!IsFlowActive(flow) || flow.IsSuspended)
                    continue;

                CozyExecutionFrame frame = flow.ActiveFrame;
                if (!frame.IsWaiting || frame.RemainingWaitSeconds < 0f)
                    continue;

                frame.RemainingWaitSeconds -= Math.Max(0f, deltaTime);
                if (frame.RemainingWaitSeconds > 0f)
                    continue;

                string outputPortName = frame.TimedWaitOutputPortName;
                frame.RemainingWaitSeconds = -1f;
                frame.TimedWaitOutputPortName = null;
                frame.IsWaiting = false;

                if (string.IsNullOrEmpty(outputPortName))
                {
                    Debug.LogError("Timed wait did not choose a flow output.");
                    StopGraph();
                    break;
                }

                ActivateFlow(flow);
                if (TryAdvanceFlow(flow, CozyNodeExecutionResult.ContinueWith(outputPortName)))
                    RunFlow(flow);
            }

            RestoreDefaultScope();
        }

        private bool StartPrimaryEntry(
            CozyGraphScope scope,
            CozyGraphEntryKind kind)
        {
            if (!IsScopeActive(scope) || scope.PrimaryFlow != null)
                return false;

            if (!TryGetEntry(scope, kind, null, false, out RuntimeCozyGraphEntry entry))
                return false;

            return StartFlow(scope, entry, GetEntryKey(kind, null, entry), true);
        }

        private bool StartAuxiliaryEntry(
            CozyGraphScope scope,
            CozyGraphEntryKind kind,
            string entryName,
            bool reportMissingEntry)
        {
            if (!IsScopeActive(scope))
                return false;

            if (!TryGetEntry(scope, kind, entryName, reportMissingEntry, out RuntimeCozyGraphEntry entry))
                return false;

            string entryKey = GetEntryKey(kind, entryName, entry);
            if (FindActiveFlow(scope, entryKey) != null)
                return false;

            return StartFlow(scope, entry, entryKey, false);
        }

        private bool TryGetEntry(
            CozyGraphScope scope,
            CozyGraphEntryKind kind,
            string entryName,
            bool reportMissingEntry,
            out RuntimeCozyGraphEntry entry)
        {
            entry = null;
            if (scope?.Graph == null)
            {
                Debug.LogWarning("Cozy Graph has not been initialized.");
                return false;
            }

            if (!scope.Graph.TryGetEntry(kind, entryName, out entry))
            {
                if (reportMissingEntry)
                    Debug.LogWarning($"On Event '{entryName}' was not found in this graph.");

                return false;
            }

            if (string.IsNullOrEmpty(entry.TargetNodeID) ||
                !scope.NodeLookup.TryGetValue(entry.TargetNodeID, out _))
            {
                Debug.LogWarning("Entry node was not found: " + entry.TargetNodeID);
                return false;
            }

            return true;
        }

        private bool StartFlow(
            CozyGraphScope scope,
            RuntimeCozyGraphEntry entry,
            string entryKey,
            bool isPrimary)
        {
            if (!scope.NodeLookup.TryGetValue(entry.TargetNodeID, out RuntimeCozyNode entryNode))
                return false;

            CozyExecutionFlow flow = new CozyExecutionFlow(
                scope,
                entryNode,
                entry.TargetInputPortName,
                entryKey);
            activeFlows.Add(flow);

            if (isPrimary)
                scope.PrimaryFlow = flow;

            RunFlow(flow);
            return true;
        }

        /// <summary>
        /// Iteratively runs one flow until it waits, starts a child graph, or
        /// ends. The loop avoids growing the C# stack for an ordinary long
        /// sequence of graph nodes.
        /// </summary>
        private void RunFlow(CozyExecutionFlow flow)
        {
            while (IsFlowActive(flow) && !flow.IsSuspended)
            {
                CozyExecutionFrame frame = flow.ActiveFrame;
                if (frame.CurrentNode == null)
                {
                    EndFlow(flow);
                    return;
                }

                ActivateFlow(flow);
                FlowNodeExecuting?.Invoke(frame.CurrentNode);
                CozyNodeExecutionResult result = frame.CurrentNode.Run(
                    session,
                    frame.EnteredFlowInputName);

                if (result == null)
                {
                    Debug.LogError($"Node '{frame.CurrentNode.NodeType}' returned no execution result.");
                    StopGraph();
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.EnterSubgraph)
                {
                    TryEnterSubgraph(flow, result);
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.ReturnFromSubgraph)
                {
                    TryReturnFromSubgraph(flow, result);
                    return;
                }

                if (result.ExecutionState == CozyNodeExecutionResult.State.Waiting)
                {
                    if (result.IsTimedWait)
                    {
                        if (string.IsNullOrEmpty(result.OutputPortName))
                        {
                            Debug.LogError(
                                $"Node '{frame.CurrentNode.NodeType}' started a timed wait without choosing a flow output.");
                            StopGraph();
                            return;
                        }

                        frame.RemainingWaitSeconds = result.WaitDurationSeconds;
                        frame.TimedWaitOutputPortName = result.OutputPortName;
                    }

                    frame.IsWaiting = true;
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

                if (!TryAdvanceFlow(flow, result))
                    return;
            }
        }

        private bool TryAdvanceFlow(
            CozyExecutionFlow flow,
            CozyNodeExecutionResult result)
        {
            CozyExecutionFrame frame = flow.ActiveFrame;
            bool isCall = result.ExecutionState == CozyNodeExecutionResult.State.Call;
            bool exitsScope = result.ExecutionState == CozyNodeExecutionResult.State.ExitScope;

            if (exitsScope && !TryExitScope(frame, frame.CurrentNode))
            {
                Debug.LogError(
                    $"Node '{frame.CurrentNode.NodeType}' tried to exit a flow scope that is not active.");
                StopGraph();
                return false;
            }

            if (string.IsNullOrEmpty(result.OutputPortName))
            {
                Debug.LogError($"Node '{frame.CurrentNode.NodeType}' continued without choosing a flow output.");
                StopGraph();
                return false;
            }

            if (!frame.CurrentNode.TryGetFlowOutput(
                    result.OutputPortName,
                    out RuntimeFlowConnection flowConnection))
            {
                if (isCall)
                {
                    frame.EnteredFlowInputName = null;
                    return true;
                }

                if (TryResumeContinuation(frame))
                    return true;

                if (!flow.Scope.IsRoot && flow.Scope.PrimaryFlow == flow)
                {
                    Debug.LogError(
                        $"Child graph '{flow.Scope.Graph.name}' ended without reaching a flow output gateway.");
                    StopGraph();
                    return false;
                }

                if (flow.Scope.IsRoot && flow.Scope.PrimaryFlow == flow)
                {
                    StopGraph();
                    return false;
                }

                // An unconnected output is a normal end for an auxiliary
                // entry such as an On Event handler.
                EndFlow(flow);
                return false;
            }

            RuntimeCozyNode callingNode = frame.CurrentNode;
            if (!frame.NodeLookup.TryGetValue(flowConnection.NextNodeID, out RuntimeCozyNode nextNode))
            {
                Debug.LogError($"Flow output '{result.OutputPortName}' points to a missing node.");
                StopGraph();
                return false;
            }

            if (isCall)
                frame.ContinuationNodes.Push(callingNode);

            frame.CurrentNode = nextNode;
            frame.EnteredFlowInputName = string.IsNullOrWhiteSpace(flowConnection.NextInputPortName)
                ? "in"
                : flowConnection.NextInputPortName;
            return true;
        }

        private static bool TryResumeContinuation(CozyExecutionFrame frame)
        {
            if (frame.ContinuationNodes.Count == 0)
                return false;

            frame.CurrentNode = frame.ContinuationNodes.Pop();
            frame.EnteredFlowInputName = null;
            return true;
        }

        private bool TryEnterSubgraph(
            CozyExecutionFlow parentFlow,
            CozyNodeExecutionResult result)
        {
            if (result.Subgraph == null || string.IsNullOrEmpty(result.TargetNodeID))
            {
                Debug.LogError("A subgraph call did not provide a valid imported child entry.");
                StopGraph();
                return false;
            }

            if (parentFlow.ActiveFrame.CurrentNode is not RuntimeCozySubgraphNode subgraphNode)
            {
                Debug.LogError("A subgraph call did not originate from a RuntimeCozySubgraphNode.");
                StopGraph();
                return false;
            }

            CozyRuntimeVariables childVariables =
                new CozyRuntimeVariables(result.Subgraph.VariableDefinitions);
            if (!subgraphNode.TryPopulateChildVariables(
                    session,
                    childVariables,
                    out string dataInputError))
            {
                Debug.LogError(dataInputError);
                StopGraph();
                return false;
            }

            CozyGraphScope childScope = new CozyGraphScope(
                result.Subgraph,
                childVariables,
                parentFlow.Scope,
                parentFlow,
                parentFlow.ActiveFrame,
                subgraphNode);
            if (!childScope.NodeLookup.TryGetValue(result.TargetNodeID, out RuntimeCozyNode entryNode))
            {
                Debug.LogError(
                    $"Subgraph '{result.Subgraph.name}' points to a missing entry node.");
                StopGraph();
                return false;
            }

            parentFlow.IsSuspended = true;
            activeScopes.Add(childScope);

            CozyExecutionFlow childPrimaryFlow = new CozyExecutionFlow(
                childScope,
                entryNode,
                result.TargetInputPortName,
                "Primary");
            childScope.PrimaryFlow = childPrimaryFlow;
            activeFlows.Add(childPrimaryFlow);
            RunFlow(childPrimaryFlow);
            return true;
        }

        private bool TryReturnFromSubgraph(
            CozyExecutionFlow flow,
            CozyNodeExecutionResult result)
        {
            CozyGraphScope childScope = flow.Scope;
            if (childScope.IsRoot || childScope.CallingFlow == null ||
                childScope.CallingFrame == null || childScope.CallingNode == null)
            {
                Debug.LogError("A subgraph flow output was reached without a calling graph.");
                StopGraph();
                return false;
            }

            if (childScope.PrimaryFlow != flow)
            {
                Debug.LogError(
                    "An On Event flow reached a Subgraph output. Only the " +
                    "subgraph's primary flow may return to its parent.");
                EndFlow(flow);
                return false;
            }

            if (string.IsNullOrEmpty(result.OutputPortName))
            {
                Debug.LogError("A subgraph flow output has no name.");
                StopGraph();
                return false;
            }

            CozyExecutionFlow parentFlow = childScope.CallingFlow;
            if (!IsFlowActive(parentFlow) || !parentFlow.IsSuspended ||
                parentFlow.ActiveFrame.CurrentNode != childScope.CallingNode)
            {
                Debug.LogError("A subgraph returned after its calling flow was no longer active.");
                StopGraph();
                return false;
            }

            if (!childScope.CallingNode.TryCollectChildOutputValues(
                    session,
                    childScope.Variables,
                    out Dictionary<string, object> outputValues,
                    out string dataOutputError))
            {
                Debug.LogError(dataOutputError);
                StopGraph();
                return false;
            }

            childScope.CallingFrame.SetSubgraphOutputValues(
                childScope.CallingNode,
                outputValues);
            CancelScope(childScope);

            parentFlow.IsSuspended = false;
            if (!IsFlowActive(parentFlow))
                return false;

            ActivateFlow(parentFlow);
            if (!TryAdvanceFlow(
                    parentFlow,
                    CozyNodeExecutionResult.ContinueWith(result.OutputPortName)))
            {
                return false;
            }

            RunFlow(parentFlow);
            return true;
        }

        internal bool TryGetSubgraphOutputValue(
            RuntimeCozySubgraphNode subgraphNode,
            string outputPortName,
            out object value)
        {
            CozyExecutionFlow activeFlow = session.ActiveExecutionFlow;
            if (activeFlow == null)
            {
                value = null;
                return false;
            }

            return activeFlow.ActiveFrame.TryGetSubgraphOutputValue(
                subgraphNode,
                outputPortName,
                out value);
        }

        private bool TryExitScope(
            CozyExecutionFrame frame,
            RuntimeCozyNode scopeNode)
        {
            bool isActiveScope = false;
            foreach (RuntimeCozyNode continuationNode in frame.ContinuationNodes)
            {
                if (continuationNode == scopeNode)
                {
                    isActiveScope = true;
                    break;
                }
            }

            if (!isActiveScope)
                return false;

            while (frame.ContinuationNodes.Count > 0)
            {
                RuntimeCozyNode cancelledNode = frame.ContinuationNodes.Pop();
                cancelledNode?.CancelFlowScope(session);

                if (cancelledNode == scopeNode)
                    return true;
            }

            return false;
        }

        internal void Stop()
        {
            StopGraph();
        }

        private void StopGraph()
        {
            List<CozyExecutionFlow> flows =
                new List<CozyExecutionFlow>(activeFlows);
            foreach (CozyExecutionFlow flow in flows)
                EndFlow(flow);

            activeFlows.Clear();
            activeScopes.Clear();
            if (rootScope != null)
            {
                rootScope.PrimaryFlow = null;
                activeScopes.Add(rootScope);
            }

            isCompleted = true;
            RestoreDefaultScope();
        }

        private void CancelScope(CozyGraphScope scope)
        {
            List<CozyExecutionFlow> flows = new List<CozyExecutionFlow>();
            foreach (CozyExecutionFlow candidate in activeFlows)
            {
                if (IsInScopeTree(candidate.Scope, scope))
                    flows.Add(candidate);
            }

            foreach (CozyExecutionFlow flow in flows)
                EndFlow(flow);

            activeScopes.RemoveAll(candidate => IsInScopeTree(candidate, scope));
        }

        private void EndFlow(CozyExecutionFlow flow)
        {
            if (!IsFlowActive(flow))
                return;

            ActivateFlow(flow);
            CozyExecutionFrame frame = flow.ActiveFrame;
            frame.CurrentNode?.CancelFlowScope(session);

            while (frame.ContinuationNodes.Count > 0)
                frame.ContinuationNodes.Pop()?.CancelFlowScope(session);

            frame.ResetFlowState();
            activeFlows.Remove(flow);
            if (flow.Scope.PrimaryFlow == flow)
                flow.Scope.PrimaryFlow = null;
        }

        private void ActivateFlow(CozyExecutionFlow flow)
        {
            session.ActivateExecutionFlow(flow);
        }

        private void RestoreDefaultScope()
        {
            session.ActivateExecutionFlow(null, rootScope?.Variables);
        }

        private bool IsScopeActive(CozyGraphScope scope)
        {
            return scope != null && activeScopes.Contains(scope);
        }

        private bool IsFlowActive(CozyExecutionFlow flow)
        {
            return flow != null && activeFlows.Contains(flow);
        }

        private CozyExecutionFlow FindActiveFlow(
            CozyGraphScope scope,
            string entryKey)
        {
            foreach (CozyExecutionFlow flow in activeFlows)
            {
                if (flow.Scope == scope && flow.EntryKey == entryKey)
                    return flow;
            }

            return null;
        }

        private static bool IsInScopeTree(
            CozyGraphScope candidate,
            CozyGraphScope root)
        {
            for (CozyGraphScope scope = candidate; scope != null; scope = scope.ParentScope)
            {
                if (scope == root)
                    return true;
            }

            return false;
        }

        private static string GetEntryKey(
            CozyGraphEntryKind kind,
            string entryName,
            RuntimeCozyGraphEntry entry)
        {
            return kind + ":" + (entryName ?? string.Empty) + ":" +
                   (entry?.TargetNodeID ?? string.Empty) + ":" +
                   (entry?.TargetInputPortName ?? string.Empty);
        }
    }
}
