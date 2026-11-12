using System;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Unity host for a CozyGraphSession. Use this component when a scene should
    /// start and update a graph automatically; game code can instead create a
    /// CozyGraphSession directly without a GameObject.
    /// </summary>
    [AddComponentMenu("Cozy Nodes/Graphs/Graph Manager")]
    public class CozyManager : MonoBehaviour
    {
        [SerializeField] private RuntimeCozyGraph RuntimeGraph;

        /// <summary>The graph asset this Unity host runs.</summary>
        public RuntimeCozyGraph Graph => RuntimeGraph;

        public CozyGraphSession Session { get; private set; }
        public event Action<CozyEvent> EventInvoked;
        public event Action<RuntimeCozyGraph, string> FlowNodeExecuting;

        internal static event Action<CozyManager, RuntimeCozyGraph, string> AnyFlowNodeExecuting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticExecutionObservers()
        {
            AnyFlowNodeExecuting = null;
        }

        private void Awake()
        {
            Session = new CozyGraphSession(RuntimeGraph);
            Session.EventInvoked += HandleEventInvoked;
            Session.FlowNodeExecuting += HandleFlowNodeExecuting;
        }

        private void Start()
        {
            Session?.RunGraph();
        }

        private void Update()
        {
            Session?.Update(Time.deltaTime);
        }

        private void LateUpdate()
        {
            Session?.LateUpdate();
        }

        private void OnDestroy()
        {
            if (Session == null)
                return;

            Session.EventInvoked -= HandleEventInvoked;
            Session.FlowNodeExecuting -= HandleFlowNodeExecuting;
            Session = null;
        }

        /// <summary>Runs this graph's default On Graph Start entry.</summary>
        public bool RunGraph()
        {
            return Session != null && Session.RunGraph();
        }

        public void Trigger(string triggerName)
        {
            Session?.Trigger(triggerName);
        }

        /// <summary>
        /// Delivers a named event to this graph's active waits and On Event
        /// entries. Use this when code targets one known graph host; use
        /// CozyGraphGlobalEvents.Emit for a broadcast to every live session.
        /// </summary>
        public void DispatchEvent(string eventName)
        {
            Session?.DispatchEvent(eventName);
        }

        public bool RunCustomEvent(string eventName)
        {
            if (Session == null)
            {
                Debug.LogWarning("Cozy Graph is not ready yet.");
                return false;
            }

            return Session.RunCustomEvent(eventName);
        }

        public void InvokeEvent(CozyEvent cozyEvent)
        {
            Session?.InvokeEvent(cozyEvent);
        }

        public bool TryGetBlackboardValue(string variableID, out object value)
        {
            if (Session == null)
            {
                value = null;
                return false;
            }

            return Session.TryGetBlackboardValue(variableID, out value);
        }

        public bool TrySetBlackboardValue(
            string variableID,
            object value,
            out string errorMessage)
        {
            if (Session == null)
            {
                errorMessage = "Cozy Graph is not ready yet.";
                return false;
            }

            return Session.TrySetBlackboardValue(variableID, value, out errorMessage);
        }

        private void HandleEventInvoked(CozyEvent cozyEvent)
        {
            EventInvoked?.Invoke(cozyEvent);
        }

        private void HandleFlowNodeExecuting(RuntimeCozyGraph graph, string nodeID)
        {
            FlowNodeExecuting?.Invoke(graph, nodeID);
            AnyFlowNodeExecuting?.Invoke(this, graph, nodeID);
        }
    }
}
