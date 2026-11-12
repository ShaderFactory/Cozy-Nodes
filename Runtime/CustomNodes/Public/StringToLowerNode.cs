namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String To Lower", Tooltip = "Text in lowercase.")]
    public class StringToLowerNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Output]
        public string Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            return (context.GetInputValue<string>(nameof(Text)) ?? string.Empty).ToLowerInvariant();
        }
    }
}
