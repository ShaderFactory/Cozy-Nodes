using System;
using System.Globalization;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    [CozyNodeMenu("Utilities/String", "String Format")]
    [CozyNodeWidth(270)]
    public class StringFormatNode : CozyStringNode
    {
        [Option("Argument Count", Tooltip = "How many format arguments this node shows.")]
        public int ArgumentCount = 1;

        [Input]
        public string Format;

        [InputList(
            nameof(ArgumentCount),
            "Argument",
            Tooltip = "Values inserted into placeholders such as {0} and {1}.",
            MinimumCount = 0,
            MaximumCount = 16)]
        public string[] Arguments;

        [Output]
        public string Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            string format = context.GetInputValue<string>(nameof(Format)) ?? string.Empty;
            try
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    format,
                    context.GetInputValues<string>(nameof(Arguments)).ToArray());
            }
            catch (FormatException)
            {
                Debug.LogError("String Format received an invalid format string.");
                return format;
            }
        }
    }
}
