using System.Text;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// A core value node that joins a configurable number of text inputs into
    /// Result. It uses only the public CozyNode API available to game projects.
    /// </summary>
    [CozyNodeMenu("Utilities/String", "String Combine")]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/CombineStrings.png")]
    [CozyNodeWidth(270)]
    public class CombineStringNode : CozyStringNode
    {
        [Option("Input Count", Tooltip = "How many text inputs this node shows.")]
        public int InputCount = 2;

        [InputList(
            nameof(InputCount),
            "Text",
            Tooltip = "Text added to the combined result.",
            MinimumCount = 2,
            MaximumCount = 16)]
        public string[] Text;

        [Output]
        public string Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            StringBuilder result = new StringBuilder();
            foreach (string text in context.GetInputValues<string>(nameof(Text)))
                result.Append(text ?? string.Empty);

            return result.ToString();
        }
    }
}
