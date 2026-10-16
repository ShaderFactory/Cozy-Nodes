namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String Ends With")]
    public class StringEndsWithNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Input]
        public string Value;

        [Output]
        public bool Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            string text = context.GetInputValue<string>(nameof(Text)) ?? string.Empty;
            string value = context.GetInputValue<string>(nameof(Value)) ?? string.Empty;
            return text.EndsWith(value, System.StringComparison.Ordinal);
        }
    }
}
