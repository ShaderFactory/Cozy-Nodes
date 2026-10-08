using System.Text;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Samples
{
    /// <summary>
    /// Your first custom Cozy value node.
    ///
    /// Copy this class into the game's Assets folder, rename it, and change the
    /// fields and GetValue method to make a new node. Cozy reads the attributes
    /// below to create the menu entry and ports automatically.
    /// </summary>
    [CozyNodeMenu("Examples/Text", "Repeat Text", Tooltip = "Repeats a text value a chosen number of times.")]
    public class RepeatTextExampleNode : CozyNode
    {
        // [Input] creates an input port. A value typed into the node is used
        // unless another node is connected to this port.
        [Input("Text")]
        [CozyTextArea(3, 5)]
        public string Text = "Cozy! ";

        // Inputs can use the normal C# types supported by Cozy Nodes.
        [Input("Times")]
        public int Times = 2;

        // [Output] creates a port that other nodes can read from.
        [Output("Result")]
        public string Result;

        // Value nodes override GetValue. Cozy calls this only when another node
        // asks for one of its outputs; it does not need execution ports.
        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            // A node can have several outputs. Return a value only for Result.
            if (outputPortName != nameof(Result))
                return null;

            // GetInputValue follows any connected wires automatically. If there
            // is no wire, it returns the value written directly on this node.
            string text = context.GetInputValue<string>(nameof(Text));
            int times = context.GetInputValue<int>(nameof(Times));

            // This is the actual behavior of this particular node.
            StringBuilder result = new StringBuilder();
            for (int index = 0; index < times; index++)
                result.Append(text);

            return result.ToString();
        }
    }
}
