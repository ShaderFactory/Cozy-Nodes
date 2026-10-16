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
    public class ReflectedCozyRuntimeNode : RuntimeCozyNode
    {
        /// <summary>
        /// Assembly-qualified type name selected in the Editor. A string keeps the
        /// imported runtime asset independent from the game's concrete assembly.
        /// </summary>
        public string CustomNodeTypeName;

        /// <summary>
        /// Values chosen in the node's Options section when the graph was
        /// imported. They stay on the runtime node so each graph instance keeps
        /// its own configuration for the same authored CozyNode class.
        /// </summary>
        public List<RuntimeCozyNodeOption> Options = new List<RuntimeCozyNodeOption>();

        public override object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null)
                return null;

            // Inputs belong to the imported runtime graph. Let the base class read
            // their local value, Blackboard variable, or connected source port.
            // Only outputs are calculated by the game-authored CozyNode class.
            if (!OutputPorts.Contains(port))
                return base.GetValue(port, cozyManager);

            CozyNode customNode = GetCustomNode(cozyManager);
            if (customNode == null)
                return base.GetValue(port, cozyManager);

            CozyNodeContext context = new CozyNodeContext(this, cozyManager);
            return customNode.GetValue(context, port.Key);
        }

        public override CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            return Run(cozyManager, null);
        }

        public override CozyNodeExecutionResult Run(
            CozyManager cozyManager,
            string enteredFlowInputName)
        {
            CozyNode customNode = GetCustomNode(cozyManager);
            if (customNode == null)
            {
                return CozyNodeExecutionResult.Fail(
                    $"Could not create custom Cozy node '{CustomNodeTypeName}'.");
            }

            CozyNodeContext context = new CozyNodeContext(
                this,
                cozyManager,
                enteredFlowInputName);
            CozyNodeExecutionResult result = customNode.Execute(context);
            if (result == null)
            {
                return CozyNodeExecutionResult.Fail(
                    $"Custom Cozy node '{CustomNodeTypeName}' returned no execution result.");
            }

            return result;
        }

        public override void CancelFlowScope(CozyManager cozyManager)
        {
            CozyNode customNode = GetCustomNode(cozyManager);
            customNode?.CancelFlowScope();
        }

        /// <summary>
        /// Adds or replaces an imported node option.
        /// </summary>
        public void RegisterOption(string key, object value, Type valueType)
        {
            foreach (RuntimeCozyNodeOption option in Options)
            {
                if (option.Key != key)
                    continue;

                option.SetValue(value, valueType);
                return;
            }

            RuntimeCozyNodeOption newOption = new RuntimeCozyNodeOption();
            newOption.Key = key;
            newOption.SetValue(value, valueType);
            Options.Add(newOption);
        }

        public override bool TryGetOptionValue(string optionName, out object value)
        {
            foreach (RuntimeCozyNodeOption option in Options)
            {
                if (option.Key != optionName)
                    continue;

                value = option.GetValue();
                return value != null;
            }

            value = null;
            return false;
        }

        private CozyNode GetCustomNode(CozyManager cozyManager)
        {
            if (cozyManager == null)
            {
                Debug.LogError("A custom Cozy node needs a running CozyManager.");
                return null;
            }

            Type nodeType = CozyNodeReflection.FindNodeType(CustomNodeTypeName);
            if (nodeType == null)
            {
                Debug.LogError(
                    $"Custom Cozy node type '{CustomNodeTypeName}' could not be found. " +
                    "Reimport the graph after fixing the missing class.");
                return null;
            }

            return cozyManager.GetOrCreateCustomNode(NodeID, nodeType);
        }
    }
}
