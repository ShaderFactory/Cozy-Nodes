using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Combines two string inputs when another node asks for Result.
    /// It does not run by itself because it is a value node, not an execution node.
    /// </summary>
    [Serializable]
    public class CombineStringRuntime : RuntimeCozyNode
    {
        public override object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null || port.Key != "Result")
                return base.GetValue(port, cozyManager);

            string first = GetInputValue("First", cozyManager) as string;
            string second = GetInputValue("Second", cozyManager) as string;

            // Empty text is friendlier than null when a field has not been filled in yet.
            if (first == null)
                first = "";

            if (second == null)
                second = "";

            return first + second;
        }
    }
}
