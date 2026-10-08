using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runtime behavior for BranchNode. It reads Condition through the normal
    /// recursive input evaluator, then selects one named flow output.
    /// </summary>
    [Serializable]
    public class BranchRuntime : RuntimeCozyNode
    {
        public override CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            object conditionValue = GetInputValue("Condition", cozyManager);
            bool conditionIsTrue = conditionValue is bool value && value;

            if (conditionIsTrue)
                return CozyNodeExecutionResult.ContinueWith("True");

            return CozyNodeExecutionResult.ContinueWith("False");
        }
    }
}
