using System;
using System.Collections.Generic;
using ShaderFactory.CozyNodes.Runtime;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor.GraphVisualization;

namespace ShaderFactory.CozyNodes.Editor
{
    /// <summary>
    /// Bridges Cozy's runtime flow notifications to Graph Toolkit's supported
    /// visualization API. It never reaches into Graph Toolkit's internal views
    /// and has no effect when the authored graph is not open.
    /// </summary>
    [InitializeOnLoad]
    internal static class CozyGraphExecutionVisualizer
    {
        private const double PulseDurationSeconds = 0.28;
        private const float PulseAnimationSpeed = 3f;

        private sealed class ActivePulse
        {
            public Context Context;
            public NodeReference Node;
            public double EndsAt;
        }

        private static readonly Dictionary<string, ActivePulse> activePulses =
            new Dictionary<string, ActivePulse>();
        private static readonly List<string> expiredPulseKeys = new List<string>();

        static CozyGraphExecutionVisualizer()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        // Subscribe after the runtime package clears static subscribers and before
        // scene objects run. This also works when Fast Enter Play Mode is enabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SubscribeToRuntimeExecution()
        {
            CozyManager.AnyFlowNodeExecuting -= HandleFlowNodeExecuting;
            CozyManager.AnyFlowNodeExecuting += HandleFlowNodeExecuting;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SubscribeToRuntimeExecution();
                return;
            }

            if (state != PlayModeStateChange.ExitingPlayMode &&
                state != PlayModeStateChange.EnteredEditMode)
                return;

            CozyManager.AnyFlowNodeExecuting -= HandleFlowNodeExecuting;
            StopAllPulses();
        }

        private static void HandleFlowNodeExecuting(
            CozyManager manager,
            RuntimeCozyGraph runtimeGraph,
            string nodeID)
        {
            if (runtimeGraph == null ||
                string.IsNullOrEmpty(runtimeGraph.SourceGraphID) ||
                string.IsNullOrEmpty(nodeID))
                return;

            if (!TryParseHash128(runtimeGraph.SourceGraphID, out Hash128 graphID) ||
                !TryParseHash128(nodeID, out Hash128 graphNodeID))
                return;

            Context context = Registry.GetActiveContext(graphID);
            if (context == null || !context.IsValid || !context.IsGraphLoaded)
                return;

            NodeReference node = context.GetNodeReference(graphNodeID);
            context.Motion.Play(node, PulseAnimationSpeed);

            string pulseKey = context.VisualizationContextID + ":" + graphNodeID;
            if (!activePulses.TryGetValue(pulseKey, out ActivePulse pulse))
            {
                pulse = new ActivePulse
                {
                    Context = context,
                    Node = node
                };
                activePulses.Add(pulseKey, pulse);
            }

            pulse.EndsAt = EditorApplication.timeSinceStartup + PulseDurationSeconds;
        }

        private static bool TryParseHash128(string value, out Hash128 hash)
        {
            try
            {
                hash = Hash128.Parse(value);
                return true;
            }
            catch (ArgumentException)
            {
                hash = default;
                return false;
            }
        }

        private static void Update()
        {
            if (activePulses.Count == 0)
                return;

            double now = EditorApplication.timeSinceStartup;
            expiredPulseKeys.Clear();

            foreach (KeyValuePair<string, ActivePulse> pair in activePulses)
            {
                ActivePulse pulse = pair.Value;
                if (now < pulse.EndsAt && pulse.Context.IsValid && pulse.Context.IsGraphLoaded)
                    continue;

                if (pulse.Context.IsValid)
                    pulse.Context.Motion.Stop(pulse.Node);

                expiredPulseKeys.Add(pair.Key);
            }

            foreach (string key in expiredPulseKeys)
                activePulses.Remove(key);
        }

        private static void StopAllPulses()
        {
            foreach (ActivePulse pulse in activePulses.Values)
            {
                if (pulse.Context.IsValid)
                    pulse.Context.Motion.Stop(pulse.Node);
            }

            activePulses.Clear();
            expiredPulseKeys.Clear();
        }
    }
}
