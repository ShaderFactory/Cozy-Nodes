using System;

namespace ShaderFactory.CozyNodes.Runtime
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

            int firstValue = CozyIntegerValue.ReadInput(this, "A", cozyManager);
            int secondValue = CozyIntegerValue.ReadInput(this, "B", cozyManager);
            return firstValue + secondValue;
        }
    }
}
