using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Runtime bridge for a game-authored CozyNode. It owns no game behavior of
    /// its own; it simply gives the custom class access to Cozy's normal input
    /// resolver and execution contract.
    /// </summary>
    [Serializable]
    internal class ReflectedCozyRuntimeNode : RuntimeCozyAuthoredElement, ICozyTriggerListener
    {
        public override object GetValue(CozyRuntimePort port, CozyGraphSession session)
        {
            if (port == null)
                return null;

            // Inputs belong to the imported runtime graph. Let the base class read
            // their local value, Blackboard variable, or connected source port.
            // Only outputs are calculated by the game-authored CozyNode class.
            if (!OutputPorts.Contains(port))
                return base.GetValue(port, session);

            CozyNode customNode = GetCustomNode(session);
            if (customNode == null)
                return base.GetValue(port, session);

            CozyNodeContext context = new CozyNodeContext(this, session);
            return CozyValueTypeRegistry.CloneForRuntime(customNode.GetValue(context, port.Key));
        }

        public override CozyNodeExecutionResult Run(CozyGraphSession session)
        {
            return Run(session, null);
        }

        public override CozyNodeExecutionResult Run(
            CozyGraphSession session,
            string enteredFlowInputName)
        {
            CozyNode customNode = GetCustomNode(session);
            if (customNode == null)
            {
                return CozyNodeExecutionResult.Fail(
                    $"Could not create custom Cozy node '{AuthoredTypeName}'.");
            }

            CozyNodeContext context = new CozyNodeContext(
                this,
                session,
                enteredFlowInputName);
            CozyNodeExecutionResult result = customNode.Execute(context);
            if (result == null)
            {
                return CozyNodeExecutionResult.Fail(
                    $"Custom Cozy node '{AuthoredTypeName}' returned no execution result.");
            }

            return result;
        }

        public override void CancelFlowScope(CozyGraphSession session)
        {
            CozyNode customNode = GetCustomNode(session);
            customNode?.CancelFlowScope();
        }

        /// <summary>
        /// Forwards an external trigger to a public CozyNode that explicitly
        /// opts in to receiving one. The graph runner continues to deal only
        /// with runtime nodes, while the authored node keeps its behavior in
        /// the same public API as every other custom node.
        /// </summary>
        public bool TryReceiveTrigger(string triggerName, CozyGraphSession session)
        {
            CozyNode customNode = GetCustomNode(session);
            if (customNode is not ICozyNodeTriggerListener triggerListener)
                return false;

            CozyNodeContext context = new CozyNodeContext(this, session);
            return triggerListener.TryReceiveTrigger(triggerName, context);
        }

        private CozyNode GetCustomNode(CozyGraphSession session)
        {
            if (session == null)
            {
                Debug.LogError("A custom Cozy node needs a running CozyGraphSession.");
                return null;
            }

            Type nodeType = CozyNodeReflection.FindNodeType(AuthoredTypeName);
            if (nodeType == null)
            {
                Debug.LogError(
                    $"Custom Cozy node type '{AuthoredTypeName}' could not be found. " +
                    "Reimport the graph after fixing the missing class.");
                return null;
            }

            // Imported node instances, rather than their editor-local IDs, own
            // custom-node state. That keeps identical node IDs in future child
            // graphs from sharing one mutable custom node instance.
            return session.GetOrCreateCustomNode(this, nodeType);
        }
    }
}
