using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Unity-facing entry point for one Cozy graph. It exposes the small public API
    /// used by scenes and game code, while CozyGraphRunner owns execution details.
    /// </summary>
    [AddComponentMenu("Cozy Nodes/Graph Manager")]
    public class CozyManager : MonoBehaviour
    {
        public RuntimeCozyGraph RuntimeGraph;

        /// <summary>
        /// Values local to this execution of RuntimeGraph. The graph asset supplies
        /// only defaults; this table is the safe place for future Get/Set nodes.
        /// </summary>
        public CozyRuntimeVariables RuntimeVariables { get; private set; }

        /// <summary>
        /// Raised when a graph executes an Emit Event node. Game code subscribes to
        /// this event to react without Cozy Nodes knowing any game-specific systems.
        /// </summary>
        public event Action<CozyEvent> EventInvoked;

        /// <summary>
        /// Raised immediately before one flow node executes. It is primarily used
        /// by the Editor execution visualizer, but remains runtime-safe so game
        /// code can also observe graph execution without an Editor dependency.
        /// </summary>
        public event Action<RuntimeCozyGraph, string> FlowNodeExecuting;

        /// <summary>
        /// Package-wide version of <see cref="FlowNodeExecuting"/>. Editor tooling
        /// uses this to subscribe before a scene starts, so the first executed
        /// node is visible even when a graph begins immediately in Start.
        /// </summary>
        public static event Action<CozyManager, RuntimeCozyGraph, string> AnyFlowNodeExecuting;

        private CozyGraphRunner graphRunner;

        // Custom nodes may hold temporary state while one graph execution is
        // running. Keep those instances on the Manager, never on the imported
        // asset, so two Managers running the same graph stay independent.
        private readonly Dictionary<RuntimeCozyNode, CozyNode> customNodeInstances =
            new Dictionary<RuntimeCozyNode, CozyNode>();

        // Contexts and blocks follow the same ownership rule as normal custom
        // nodes: their temporary state belongs to one running Manager, never to
        // the imported graph asset shared by scenes and Managers.
        private readonly Dictionary<RuntimeCozyContextNode, CozyContext> contextInstances =
            new Dictionary<RuntimeCozyContextNode, CozyContext>();
        private readonly Dictionary<RuntimeCozyBlock, CozyBlock> blockInstances =
            new Dictionary<RuntimeCozyBlock, CozyBlock>();

        // Fast Enter Play Mode can preserve static event subscribers. Each Play
        // session starts with a clean observer list; Editor tooling subscribes
        // again before the first scene loads.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticExecutionObservers()
        {
            AnyFlowNodeExecuting = null;
        }

        private void Awake()
        {
            graphRunner = new CozyGraphRunner(this);
            graphRunner.FlowNodeExecuting += HandleFlowNodeExecuting;
        }

        private void OnDestroy()
        {
            if (graphRunner != null)
                graphRunner.FlowNodeExecuting -= HandleFlowNodeExecuting;
        }

        private void Start()
        {
            RuntimeVariables = new CozyRuntimeVariables(RuntimeGraph?.VariableDefinitions);
            customNodeInstances.Clear();
            contextInstances.Clear();
            blockInstances.Clear();
            if (graphRunner.Initialize(RuntimeGraph))
                graphRunner.RunOnStart();
        }

        private void Update()
        {
            graphRunner?.Update(Time.deltaTime);
        }

        /// <summary>
        /// Gets the game-authored node instance associated with one imported node.
        /// This is internal package plumbing; custom nodes receive their behavior
        /// through CozyNodeContext rather than accessing this method directly.
        /// </summary>
        internal CozyNode GetOrCreateCustomNode(RuntimeCozyNode runtimeNode, Type nodeType)
        {
            if (runtimeNode == null || nodeType == null)
                return null;

            if (customNodeInstances.TryGetValue(runtimeNode, out CozyNode existingNode))
                return existingNode;

            CozyNode newNode = CozyNodeReflection.CreateNodeInstance(nodeType);
            if (newNode != null)
                customNodeInstances.Add(runtimeNode, newNode);

            return newNode;
        }

        internal CozyContext GetOrCreateContext(
            RuntimeCozyContextNode runtimeContext,
            Type contextType)
        {
            if (runtimeContext == null || contextType == null)
                return null;

            if (contextInstances.TryGetValue(runtimeContext, out CozyContext existingContext))
                return existingContext;

            CozyContext newContext = CozyNodeReflection.CreateContextInstance(contextType);
            if (newContext != null)
                contextInstances.Add(runtimeContext, newContext);

            return newContext;
        }

        internal CozyBlock GetOrCreateBlock(RuntimeCozyBlock runtimeBlock, Type blockType)
        {
            if (runtimeBlock == null || blockType == null)
                return null;

            if (blockInstances.TryGetValue(runtimeBlock, out CozyBlock existingBlock))
                return existingBlock;

            CozyBlock newBlock = CozyNodeReflection.CreateBlockInstance(blockType);
            if (newBlock != null)
                blockInstances.Add(runtimeBlock, newBlock);

            return newBlock;
        }

        /// <summary>
        /// Switches the variable table exposed to nodes while the runner enters
        /// or returns from an imported child graph. It remains internal so game
        /// code can observe values but cannot replace a running frame's scope.
        /// </summary>
        internal void SetRuntimeVariables(CozyRuntimeVariables runtimeVariables)
        {
            RuntimeVariables = runtimeVariables;
        }

        /// <summary>
        /// Retrieves a local-subgraph value that has returned into the active
        /// caller frame. RuntimeCozySubgraphNode uses this instead of retaining
        /// mutable state on the imported graph asset.
        /// </summary>
        internal bool TryGetSubgraphOutputValue(
            RuntimeCozySubgraphNode subgraphNode,
            string outputPortName,
            out object value)
        {
            if (graphRunner == null)
            {
                value = null;
                return false;
            }

            return graphRunner.TryGetSubgraphOutputValue(
                subgraphNode,
                outputPortName,
                out value);
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
        /// Starts the named On Event entry authored in this graph. This is a
        /// graph entry point, not a signal for a Wait For Event node.
        /// </summary>
        public bool RunCustomEvent(string eventName)
        {
            if (graphRunner == null)
            {
                Debug.LogWarning("Cozy Graph is not ready yet.");
                return false;
            }

            return graphRunner.RunCustomEvent(eventName);
        }

        private void HandleFlowNodeExecuting(RuntimeCozyNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.NodeID))
                return;

            RuntimeCozyGraph nodeGraph = node.Graph ?? RuntimeGraph;
            if (nodeGraph == null)
                return;

            FlowNodeExecuting?.Invoke(nodeGraph, node.NodeID);
            AnyFlowNodeExecuting?.Invoke(this, nodeGraph, node.NodeID);
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
