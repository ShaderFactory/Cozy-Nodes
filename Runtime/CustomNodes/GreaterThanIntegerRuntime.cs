using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runtime behavior for Greater Than Integer. It is a value node and evaluates
    /// only when another node requests its Result output.
    /// </summary>
    [Serializable]
    public class GreaterThanIntegerRuntime : RuntimeCozyNode
    {
        public override object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null || port.Key != "Result")
                return base.GetValue(port, cozyManager);

            int firstValue = CozyIntegerValue.ReadInput(this, "A", cozyManager);
            int secondValue = CozyIntegerValue.ReadInput(this, "B", cozyManager);
            return firstValue > secondValue;
        }
    }
}
