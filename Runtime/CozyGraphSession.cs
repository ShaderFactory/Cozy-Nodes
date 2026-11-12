using System;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Owns one isolated execution of a Cozy graph. The imported graph remains
    /// immutable data; variables, node instances, waits, and flow state belong
    /// to this session alone.
    /// </summary>
    public sealed class CozyGraphSession
    {
        private readonly CozyGraphRunner graphRunner;

        public RuntimeCozyGraph Graph { get; private set; }
        internal CozyRuntimeVariables RuntimeVariables { get; private set; }
        internal CozyExecutionFlow ActiveExecutionFlow { get; private set; }

        public event Action<RuntimeCozyGraph, string> FlowNodeExecuting;
        public event Action<CozyEvent> EventInvoked;

        public CozyGraphSession(RuntimeCozyGraph graph)
        {
            graphRunner = new CozyGraphRunner(this);
            graphRunner.FlowNodeExecuting += HandleFlowNodeExecuting;
            CozyGraphGlobalEvents.RegisterSession(this);
            Initialize(graph);
        }

        /// <summary>Resets this execution to begin using the supplied graph.</summary>
        public bool Initialize(RuntimeCozyGraph graph)
        {
            // Let the old flow release its own node state before this session
            // forgets the instances that belonged to it.
            graphRunner.Stop();
            Graph = graph;
            return graphRunner.Initialize(graph);
        }

        /// <summary>Runs the graph's default On Graph Start entry.</summary>
        public bool RunGraph() => graphRunner.RunOnStart();

        public bool RunCustomEvent(string eventName) => graphRunner.RunCustomEvent(eventName);

        /// <summary>
        /// Delivers an event both to matching Wait For Event nodes and to named
        /// On Event entries in every active scope of this graph session.
        /// </summary>
        public void DispatchEvent(string eventName) => graphRunner.DispatchEvent(eventName);

        public void Update(float deltaTime)
        {
            graphRunner.Update(deltaTime);
            graphRunner.RunOnUpdate();
        }

        public void LateUpdate() => graphRunner.RunOnLateUpdate();

        public void Trigger(string triggerName) => graphRunner.Trigger(triggerName);

        public void Stop() => graphRunner.Stop();

        /// <summary>
        /// Reads one Blackboard value from this execution. The identifier is the
        /// stable variable ID, normally obtained from an input variable port.
        /// </summary>
        public bool TryGetBlackboardValue(string variableID, out object value)
        {
            if (RuntimeVariables == null)
            {
                value = null;
                return false;
            }

            return RuntimeVariables.TryGetValue(variableID, out value);
        }

        /// <summary>
        /// Changes one Blackboard value only in this session. It never writes
        /// back into the imported graph asset.
        /// </summary>
        public bool TrySetBlackboardValue(
            string variableID,
            object value,
            out string errorMessage)
        {
            if (RuntimeVariables == null)
            {
                errorMessage = "The Cozy graph session has no Blackboard variables.";
                return false;
            }

            return RuntimeVariables.TrySetValue(variableID, value, out errorMessage);
        }

        public void InvokeEvent(CozyEvent cozyEvent)
        {
            if (cozyEvent == null)
            {
                Debug.LogWarning("Tried to invoke a missing Cozy event.");
                return;
            }

            if (string.IsNullOrWhiteSpace(cozyEvent.EventName))
            {
                Debug.LogWarning("Tried to invoke a Cozy event without a name.");
                return;
            }

            EventInvoked?.Invoke(cozyEvent);
            CozyGraphGlobalEvents.Publish(cozyEvent);
        }

        internal CozyNode GetOrCreateCustomNode(RuntimeCozyNode runtimeNode, Type nodeType)
        {
            if (ActiveExecutionFlow == null)
                return null;

            return ActiveExecutionFlow.GetOrCreateCustomNode(runtimeNode, nodeType);
        }

        internal CozyContext GetOrCreateContext(RuntimeCozyContextNode runtimeContext, Type contextType)
        {
            if (ActiveExecutionFlow == null)
                return null;

            return ActiveExecutionFlow.GetOrCreateContext(runtimeContext, contextType);
        }

        internal CozyBlock GetOrCreateBlock(RuntimeCozyBlock runtimeBlock, Type blockType)
        {
            if (ActiveExecutionFlow == null)
                return null;

            return ActiveExecutionFlow.GetOrCreateBlock(runtimeBlock, blockType);
        }

        internal void ActivateExecutionFlow(
            CozyExecutionFlow flow,
            CozyRuntimeVariables fallbackVariables = null)
        {
            ActiveExecutionFlow = flow;
            RuntimeVariables = flow?.ActiveFrame?.Variables ?? fallbackVariables;
        }

        internal bool TryGetSubgraphOutputValue(
            RuntimeCozySubgraphNode subgraphNode,
            string outputPortName,
            out object value)
        {
            return graphRunner.TryGetSubgraphOutputValue(subgraphNode, outputPortName, out value);
        }

        private void HandleFlowNodeExecuting(RuntimeCozyNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.NodeID))
                return;

            RuntimeCozyGraph nodeGraph = node.Graph ?? Graph;
            if (nodeGraph != null)
                FlowNodeExecuting?.Invoke(nodeGraph, node.NodeID);
        }
    }
}
