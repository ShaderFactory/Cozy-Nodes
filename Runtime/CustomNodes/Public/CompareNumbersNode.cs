namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Compares two numbers using the selected relationship. Its float inputs
    /// deliberately accept integer wires through Cozy's int-to-float conversion,
    /// so authors use one numeric comparison node instead of separate integer
    /// and float variants.
    /// </summary>
    [CozyNodeMenu("Utilities/Comparison", "Compare Numbers", Tooltip = "Compares A and B with the selected operation (greater, less, equal...). Accepts integers and floats.")]
    public sealed class CompareNumbersNode : CozyCompareNode<ComparisonOperation>
    {
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
