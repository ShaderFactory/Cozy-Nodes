namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Tests whether two boolean values match or differ. No ordering operations
    /// are exposed because booleans have no meaningful numeric order.
    /// </summary>
    [CozyNodeMenu("Utilities/Comparison", "Compare Boolean", Tooltip = "True when the two booleans are equal, or different, as selected in Options.")]
    public sealed class CompareBooleanNode : CozyCompareNode<BooleanComparisonOperation>
    {
        [Input]
        public bool A;

        [Input]
        public bool B;

        [Output]
        public bool Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            bool firstValue = context.GetInputValue<bool>(nameof(A));
            bool secondValue = context.GetInputValue<bool>(nameof(B));
            BooleanComparisonOperation comparison =
                context.GetOptionValue<BooleanComparisonOperation>(nameof(Comparison));

            return comparison == BooleanComparisonOperation.Equal
                ? firstValue == secondValue
                : firstValue != secondValue;
        }
    }
}
