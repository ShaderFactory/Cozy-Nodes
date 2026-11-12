namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String Is Empty", Tooltip = "True when Text has no characters.")]
    public class StringIsEmptyNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Output]
        public bool Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            return string.IsNullOrEmpty(context.GetInputValue<string>(nameof(Text)));
        }
    }
}
