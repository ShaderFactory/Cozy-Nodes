namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String To Upper", Tooltip = "Text in uppercase.")]
    public class StringToUpperNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Output]
        public string Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            return (context.GetInputValue<string>(nameof(Text)) ?? string.Empty).ToUpperInvariant();
        }
    }
}
