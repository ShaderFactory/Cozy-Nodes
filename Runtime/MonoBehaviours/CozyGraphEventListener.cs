using System;
using UnityEngine;
using UnityEngine.Events;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Translates one named event emitted by a Cozy graph into a UnityEvent.
    /// Use it to connect authored graph decisions to scene components without
    /// making the graph depend on a specific Animator, UI, or game system.
    /// </summary>
    [AddComponentMenu("Cozy Nodes/Events/Graph Event Listener")]
    public sealed class CozyGraphEventListener : MonoBehaviour
    {
        [SerializeField, Tooltip("The emitted Cozy event this listener reacts to.")]
        private string eventName;

        [SerializeField, Tooltip("Called when the matching Cozy event is emitted.")]
        private UnityEvent onEventReceived;

        private void OnEnable()
        {
            CozyGraphGlobalEvents.EventEmitted += HandleEventEmitted;
        }

        private void OnDisable()
        {
            CozyGraphGlobalEvents.EventEmitted -= HandleEventEmitted;
        }

        private void HandleEventEmitted(CozyEvent cozyEvent)
        {
            if (cozyEvent == null ||
                string.IsNullOrWhiteSpace(eventName) ||
                !string.Equals(cozyEvent.EventName, eventName, StringComparison.Ordinal))
            {
                return;
            }

            onEventReceived?.Invoke();
        }
    }
}
