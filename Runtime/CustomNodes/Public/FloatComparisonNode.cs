namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// A core value node that compares two floating-point values using the
    /// selected operation and returns the boolean result.
    /// </summary>
    [CozyNodeMenu("Utilities/Comparison", "Float Comparison")]
    public class FloatComparisonNode : CozyNode
    {
        [Option("Comparison", Tooltip = "The relationship tested between A and B.")]
        public ComparisonOperation Comparison = ComparisonOperation.GreaterThan;

        [Input]
        public float A;

        [Input]
        public float B;

        [Output]
        public bool Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            float firstValue = context.GetInputValue<float>(nameof(A));
            float secondValue = context.GetInputValue<float>(nameof(B));
            ComparisonOperation comparison =
                context.GetOptionValue<ComparisonOperation>(nameof(Comparison));

            return Compare(firstValue, secondValue, comparison);
        }

        private static bool Compare(float firstValue, float secondValue, ComparisonOperation comparison)
        {
            switch (comparison)
            {
                case ComparisonOperation.GreaterThan:
                    return firstValue > secondValue;

                case ComparisonOperation.GreaterThanOrEqual:
                    return firstValue >= secondValue;

                case ComparisonOperation.LessThan:
                    return firstValue < secondValue;

                case ComparisonOperation.LessThanOrEqual:
                    return firstValue <= secondValue;

                case ComparisonOperation.Equal:
                    return firstValue == secondValue;

                case ComparisonOperation.NotEqual:
                    return firstValue != secondValue;

                default:
                    return false;
            }
        }
    }
}
