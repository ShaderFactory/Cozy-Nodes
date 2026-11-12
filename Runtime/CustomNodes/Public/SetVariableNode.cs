namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Stores a value in the current graph instance's Blackboard. Select its
    /// Variable Type in the node UI; both inputs then use that concrete type.
    /// </summary>
    [CozyNodeMenu("Utilities/Variables", "Set Variable", Tooltip = "Stores a value in a Blackboard variable of this graph run. Choose the variable type in the node.")]
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
            if (!context.TryGetInputVariableID(
                    nameof(Variable),
                    out string variableID))
            {
                return CozyNodeExecutionResult.Fail(
                    "Set Variable needs a Blackboard variable connected to Variable.");
            }

            if (context.Session == null)
            {
                return CozyNodeExecutionResult.Fail(
                    "Set Variable needs a running CozyGraphSession.");
            }

            object value = context.GetInputValue<object>(nameof(Value));
            if (!context.Session.TrySetBlackboardValue(
                    variableID,
                    value,
                    out string errorMessage))
            {
                return CozyNodeExecutionResult.Fail("Set Variable failed: " + errorMessage);
            }

            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
