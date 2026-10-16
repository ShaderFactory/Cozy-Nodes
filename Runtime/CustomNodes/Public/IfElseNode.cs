namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Chooses one of two execution paths from a boolean condition.
    /// </summary>
    [CozyNodeMenu("Control Flow", "If/Else")]
    [FlowInput]
    [FlowOutput("True")]
    [FlowOutput("False")]
    public class IfElseNode : CozyNode
    {
        [Input]
        public bool Condition;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            bool conditionIsTrue = context.GetInputValue<bool>(nameof(Condition));
            return CozyNodeExecutionResult.ContinueWith(
                conditionIsTrue ? "True" : "False");
        }
    }
}
