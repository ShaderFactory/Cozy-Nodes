namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Tests whether two strings match or differ using ordinal, case-sensitive
    /// comparison. Substring, prefix, and suffix tests have dedicated nodes.
    /// </summary>
    [CozyNodeMenu("Utilities/Comparison", "Compare Strings", Tooltip = "True when the two texts are equal, or different, as selected in Options. Case-sensitive.")]
    public sealed class CompareStringsNode : CozyCompareNode<StringEqualityOperation>
    {
        [Input]
        public string A;

        [Input]
        public string B;

        [Output]
        public bool Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            string firstValue = context.GetInputValue<string>(nameof(A)) ?? string.Empty;
            string secondValue = context.GetInputValue<string>(nameof(B)) ?? string.Empty;
            StringEqualityOperation comparison =
                context.GetOptionValue<StringEqualityOperation>(nameof(Comparison));
            bool areEqual = string.Equals(
                firstValue,
                secondValue,
                System.StringComparison.Ordinal);

            return comparison == StringEqualityOperation.Equal ? areEqual : !areEqual;
        }
    }
}
