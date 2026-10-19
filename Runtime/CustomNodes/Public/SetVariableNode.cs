namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Stores a value in the current graph instance's Blackboard. Select its
    /// Variable Type in the node UI; both inputs then use that concrete type.
    /// </summary>
    [CozyNodeMenu("Utilities/Variables", "Set Variable")]
    [CozyTypeParameter("T", "Variable Type", AllowArrayTypes = true)]
    [FlowInput]
    [FlowOutput]
    public sealed class SetVariableNode : CozyNode
    {
        [Input("Variable")]
        [CozyBlackboardVariable]
        [CozyTypeParameterPort("T")]
        public object Variable;

        [Input]
        [CozyTypeParameterPort("T")]
        public object Value;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            if (!context.TryGetInputVariableReference(
                    nameof(Variable),
                    out RuntimeVariableReference variableReference))
            {
                return CozyNodeExecutionResult.Fail(
                    "Set Variable needs a Blackboard variable connected to Variable.");
            }

            if (context.Manager?.RuntimeVariables == null)
            {
                return CozyNodeExecutionResult.Fail(
                    "Set Variable needs a running CozyManager.");
            }

            object value = context.GetInputValue<object>(nameof(Value));
            if (!context.Manager.RuntimeVariables.TrySetValue(
                    variableReference.variableID,
                    value,
                    out string errorMessage))
            {
                return CozyNodeExecutionResult.Fail("Set Variable failed: " + errorMessage);
            }

            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
