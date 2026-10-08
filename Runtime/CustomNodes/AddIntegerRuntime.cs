using System;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runtime behavior for Add Integer. It resolves both inputs through the
    /// standard recursive evaluator, so each input can be a direct number,
    /// Blackboard variable or output from another value node.
    /// </summary>
    [Serializable]
    public class AddIntegerRuntime : RuntimeCozyNode
    {
        public override object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null || port.Key != "Result")
                return base.GetValue(port, cozyManager);

            int firstValue = GetIntegerInput("A", cozyManager);
            int secondValue = GetIntegerInput("B", cozyManager);
            return firstValue + secondValue;
        }

        /// <summary>
        /// Reads an integer input without implicitly rounding numbers. A mismatched
        /// connection is a graph-authoring problem, so the Console explains it and
        /// the node uses zero as a safe fallback for this execution.
        /// </summary>
        private int GetIntegerInput(string portName, CozyManager cozyManager)
        {
            object value = GetInputValue(portName, cozyManager);
            if (value is int integerValue)
                return integerValue;

            if (value != null)
            {
                Debug.LogWarning(
                    $"Add Integer expected an int on '{portName}', but received {value.GetType().Name}.");
            }

            return 0;
        }
    }
}
