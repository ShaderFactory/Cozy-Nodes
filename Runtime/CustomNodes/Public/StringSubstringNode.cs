using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String Substring")]
    public class StringSubstringNode : CozyStringNode
    {
        [Input]
        public string Text;

        [Input]
        public int StartIndex;

        [Input]
        public int Length = -1;

        [Output]
        public string Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            string text = context.GetInputValue<string>(nameof(Text)) ?? string.Empty;
            int startIndex = Math.Clamp(
                context.GetInputValue<int>(nameof(StartIndex)),
                0,
                text.Length);
            int requestedLength = context.GetInputValue<int>(nameof(Length));
            int length = requestedLength < 0
                ? text.Length - startIndex
                : Math.Min(requestedLength, text.Length - startIndex);

            return text.Substring(startIndex, length);
        }
    }
}
