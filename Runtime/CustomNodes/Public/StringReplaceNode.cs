namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String Replace")]
    public class StringReplaceNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Input]
        public string OldValue;

        [Input]
        public string NewValue;

        [Output]
        public string Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            string text = context.GetInputValue<string>(nameof(Text)) ?? string.Empty;
            string oldValue = context.GetInputValue<string>(nameof(OldValue)) ?? string.Empty;
            string newValue = context.GetInputValue<string>(nameof(NewValue)) ?? string.Empty;
            return oldValue.Length == 0 ? text : text.Replace(oldValue, newValue);
        }
    }
}
