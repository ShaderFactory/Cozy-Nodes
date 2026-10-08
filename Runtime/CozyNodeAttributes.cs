using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Describes where a game node appears in Cozy Nodes' type picker.
    /// This metadata is intentionally runtime-safe so the same class can be
    /// discovered in the Editor and used in a player build.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CozyNodeMenuAttribute : Attribute
    {
        public string Category { get; }
        public string Title { get; }
        public string Tooltip { get; set; }

        public CozyNodeMenuAttribute(string category, string title = null)
        {
            Category = category;
            Title = title;
        }
    }

    /// <summary>
    /// Makes a serialized field into a typed value input. When the port has no
    /// connection, its field value is the editable fallback shown in the node.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class InputAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Tooltip { get; set; }

        public InputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Makes a serialized field into a typed value output. The field acts as a
    /// named declaration; GetValue provides the output value at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class OutputAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Tooltip { get; set; }

        public OutputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Draws a string Input as a multi-line text area. It changes only the
    /// editor presentation; the field remains a normal string at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class CozyTextAreaAttribute : Attribute
    {
        public int MinLines { get; }
        public int MaxLines { get; }

        public CozyTextAreaAttribute(int minLines = 3, int maxLines = 8)
        {
            MinLines = minLines;
            MaxLines = maxLines;
        }
    }

    /// <summary>
    /// Declares a named execution input on a flow node. If Execute is overridden
    /// and no flow attributes are supplied, Cozy Nodes creates the usual in/out
    /// pair automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class FlowInputAttribute : Attribute
    {
        public string PortName { get; }

        public FlowInputAttribute(string portName = "in")
        {
            PortName = portName;
        }
    }

    /// <summary>
    /// Declares a named execution output on a flow node. Use multiple attributes
    /// for nodes such as a branch that can choose different paths.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class FlowOutputAttribute : Attribute
    {
        public string PortName { get; }

        public FlowOutputAttribute(string portName = "out")
        {
            PortName = portName;
        }
    }
}
