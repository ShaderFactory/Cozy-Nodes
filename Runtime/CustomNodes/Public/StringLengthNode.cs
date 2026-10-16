namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String Length")]
    public class StringLengthNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Output]
        public int Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            return (context.GetInputValue<string>(nameof(Text)) ?? string.Empty).Length;
        }
    }
}
