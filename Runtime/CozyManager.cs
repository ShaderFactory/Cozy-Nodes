using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Unity-facing entry point for one Cozy graph. It exposes the small public API
    /// used by scenes and game code, while CozyGraphRunner owns execution details.
    /// </summary>
    public class CozyManager : MonoBehaviour
    {
        public RuntimeCozyGraph RuntimeGraph;

        /// <summary>
        /// Values local to this execution of RuntimeGraph. The graph asset supplies
        /// only defaults; this table is the safe place for future Get/Set nodes.
        /// </summary>
        public CozyRuntimeVariables RuntimeVariables { get; private set; }

        /// <summary>
        /// Raised when a graph executes an Invoke Event node. Game code subscribes to
        /// this event to react without Cozy Nodes knowing any game-specific systems.
        /// </summary>
        public event Action<CozyEvent> EventInvoked;

        private CozyGraphRunner graphRunner;

        // Custom nodes may hold temporary state while one graph execution is
        // running. Keep those instances on the Manager, never on the imported
        // asset, so two Managers running the same graph stay independent.
        private readonly Dictionary<string, CozyNode> customNodeInstances =
            new Dictionary<string, CozyNode>();

        private void Awake()
        {
            graphRunner = new CozyGraphRunner(this);
        }

        private void Start()
        {
            RuntimeVariables = new CozyRuntimeVariables(RuntimeGraph?.VariableDefinitions);
            customNodeInstances.Clear();
            graphRunner.Start(RuntimeGraph);
        }

        /// <summary>
        /// Gets the game-authored node instance associated with one imported node.
        /// This is internal package plumbing; custom nodes receive their behavior
        /// through CozyNodeContext rather than accessing this method directly.
        /// </summary>
        internal CozyNode GetOrCreateCustomNode(string nodeID, Type nodeType)
        {
            if (string.IsNullOrWhiteSpace(nodeID) || nodeType == null)
                return null;

            if (customNodeInstances.TryGetValue(nodeID, out CozyNode existingNode))
                return existingNode;

            CozyNode newNode = CozyNodeReflection.CreateNodeInstance(nodeType);
            if (newNode != null)
                customNodeInstances.Add(nodeID, newNode);

            return newNode;
        }

        /// <summary>
        /// Sends a named event to the node currently waiting in this graph.
        /// A UI Button can call this method directly and provide its trigger name.
        /// </summary>
        public void Trigger(string triggerName)
        {
            graphRunner.Trigger(triggerName);
        }

        /// <summary>
        /// Invokes an event for the game code that owns this graph runner.
        /// Custom game nodes can also use this method without creating a dependency
        /// from the Cozy Nodes package back to the game assembly.
        /// </summary>
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
        }
    }
}
