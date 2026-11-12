using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Broadcasts a named signal to all live Cozy graph sessions. A session can
    /// resume matching Wait For Event nodes and start matching On Event entries.
    /// </summary>
    public static class CozyGraphGlobalEvents
    {
        private static readonly List<WeakReference<CozyGraphSession>> sessions =
            new List<WeakReference<CozyGraphSession>>();

        /// <summary>
        /// Raised when a graph executes an Emit Event node. This is an outgoing
        /// graph notification, separate from <see cref="Emit"/>, which delivers
        /// an incoming signal to graph sessions.
        /// </summary>
        public static event Action<CozyEvent> EventEmitted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessions()
        {
            sessions.Clear();
            EventEmitted = null;
        }

        internal static void RegisterSession(CozyGraphSession session)
        {
            if (session == null)
                return;

            RemoveExpiredSessions();
            sessions.Add(new WeakReference<CozyGraphSession>(session));
        }

        internal static void Publish(CozyEvent cozyEvent)
        {
            EventEmitted?.Invoke(cozyEvent);
        }

        /// <summary>
        /// Broadcasts a signal to every live graph session. This is deliberately
        /// separate from CozyManager.Trigger, which targets one known session.
        /// </summary>
        public static void Emit(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogWarning("A Cozy global event needs a name.");
                return;
            }

            List<CozyGraphSession> liveSessions =
                new List<CozyGraphSession>();

            for (int index = sessions.Count - 1; index >= 0; index--)
            {
                if (!sessions[index].TryGetTarget(out CozyGraphSession session))
                {
                    sessions.RemoveAt(index);
                    continue;
                }

                liveSessions.Add(session);
            }

            foreach (CozyGraphSession session in liveSessions)
                session.DispatchEvent(eventName);
        }

        private static void RemoveExpiredSessions()
        {
            for (int index = sessions.Count - 1; index >= 0; index--)
            {
                if (!sessions[index].TryGetTarget(out _))
                    sessions.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// UnityEvent and Animation Event entry point for CozyGraphGlobalEvents.
    /// Add this component to any scene object and call Emit with a fixed string.
    /// </summary>
    [AddComponentMenu("Cozy Nodes/Events/Global Event Emitter")]
    public sealed class CozyGraphGlobalEventEmitter : MonoBehaviour
    {
        public void Emit(string eventName)
        {
            CozyGraphGlobalEvents.Emit(eventName);
        }
    }

}
