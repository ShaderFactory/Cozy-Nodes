using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Describes when a graph entry is started. Entry nodes declare the places
    /// where execution can begin; they are not executable nodes themselves.
    /// </summary>
    public enum CozyGraphEntryKind
    {
        OnStart,
        CustomEvent
    }

    /// <summary>
    /// The author-selected concrete type for a Cozy node type parameter.
    /// </summary>
    public enum CozyTypeParameterSelection
    {
        String = 1,
        Int = 2,
        Float = 3,
        Bool = 4
    }

    /// <summary>
    /// The concrete value choices offered by a type parameter that explicitly
    /// allows arrays. The scalar values deliberately keep the same numeric
    /// values as <see cref="CozyTypeParameterSelection"/>, so a previously
    /// saved scalar choice remains meaningful when a node gains array support.
    /// </summary>
    public enum CozyValueTypeParameterSelection
    {
        String = 1,
        Int = 2,
        Float = 3,
        Bool = 4,
        ArrayOfString = 5,
        ArrayOfInt = 6,
        ArrayOfFloat = 7,
        ArrayOfBool = 8
    }

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
    /// Makes a field into a node option. Options configure one node instance and
    /// are stored in the graph; they are not input ports and cannot be connected.
    /// Cozy Nodes supports string, int, float, bool and enum option fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class OptionAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Tooltip { get; set; }

        public OptionAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Makes an array field into a variable number of value inputs. The number
    /// of inputs comes from an int field marked with <see cref="OptionAttribute"/>.
    /// Each generated input has a stable internal name, so existing values and
    /// connections remain attached when the count grows.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class InputListAttribute : Attribute
    {
        public string CountOptionName { get; }
        public string DisplayName { get; }
        public string Tooltip { get; set; }
        public int MinimumCount { get; set; }
        public int MaximumCount { get; set; } = 16;

        public InputListAttribute(string countOptionName, string displayName = null)
        {
            CountOptionName = countOptionName;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Declares a named type parameter on a public CozyNode. The author can
    /// change its concrete type in the node UI, and every port using that
    /// parameter is defined with the same type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class CozyTypeParameterAttribute : Attribute
    {
        public string Name { get; }
        public string DisplayName { get; }
        /// <summary>
        /// Lets T itself resolve to one of Cozy's supported one-dimensional array
        /// types. Use this for polymorphic nodes such as Set Variable; leave it
        /// false for nodes where T is an array element type.
        /// </summary>
        public bool AllowArrayTypes { get; set; }

        public CozyTypeParameterAttribute(string name, string displayName = null)
        {
            Name = name;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// Uses a node type parameter for a value port or an InputList. The backing
    /// field must be object for an ordinary port, or object[] for an InputList.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class CozyTypeParameterPortAttribute : Attribute
    {
        public string TypeParameterName { get; }
        public bool IsArray { get; set; }

        public CozyTypeParameterPortAttribute(string typeParameterName)
        {
            TypeParameterName = typeParameterName;
        }
    }

    /// <summary>
    /// Requires an input to be connected directly to a Graph Toolkit Blackboard
    /// variable. The port uses its node's selected concrete type, while the
    /// runtime receives the variable's stable reference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public sealed class CozyBlackboardVariableAttribute : Attribute
    {
    }

    /// <summary>
    /// Sets the minimum editor width, in pixels, for the input section of a
    /// public CozyNode. This is presentation metadata only; it does not change
    /// graph serialization or runtime behavior.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CozyNodeWidthAttribute : Attribute
    {
        public int MinimumWidth { get; }

        public CozyNodeWidthAttribute(int minimumWidth)
        {
            MinimumWidth = minimumWidth;
        }
    }

    /// <summary>
    /// Sets a node's default editor accent color. Color components normally use
    /// the 0-to-1 range. This affects presentation only and does not change a
    /// graph's runtime behavior.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CozyNodeColorAttribute : Attribute
    {
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public float Alpha { get; set; } = 1f;

        public CozyNodeColorAttribute(float red, float green, float blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }
    }

    /// <summary>
    /// Sets a public CozyNode's Graph Toolkit accent color independently from
    /// its header color. The accent is used for the node's border and other
    /// native Graph Toolkit visual cues; it does not recolor the node body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CozyNodeAccentColorAttribute : Attribute
    {
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public float Alpha { get; set; } = 1f;

        public CozyNodeAccentColorAttribute(float red, float green, float blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }
    }

    /// <summary>
    /// Assigns an image asset to a public CozyNode's Graph Toolkit header.
    /// The path is Unity project-relative, for example
    /// "Assets/MyGame/GraphIcons/OpenDoor.png", or package-relative, for
    /// example "Packages/com.example.package/Editor/Icons/OpenDoor.png".
    /// Use a transparent, monochrome image so it remains readable at the
    /// small size used by a node header.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CozyNodeIconAttribute : Attribute
    {
        public string AssetPath { get; }

        public CozyNodeIconAttribute(string assetPath)
        {
            AssetPath = assetPath;
        }
    }

    /// <summary>
    /// Sets the text color used in a public CozyNode's header. Use this with a
    /// light CozyNodeColor so the node title remains readable without changing
    /// the darker input and output sections.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CozyNodeHeaderTextColorAttribute : Attribute
    {
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public float Alpha { get; set; } = 1f;

        public CozyNodeHeaderTextColorAttribute(float red, float green, float blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
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

        /// <summary>
        /// Minimum editor width, in pixels, for text fields on this node. Leave
        /// this at zero to keep Graph Toolkit's normal width.
        /// </summary>
        public int MinimumWidth { get; set; }

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

    /// <summary>
    /// Marks a CozyNode as an entry point for a graph. Entry nodes only expose
    /// flow outputs; the importer records their connected targets as runtime
    /// entries instead of creating a runtime node for them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CozyGraphEntryAttribute : Attribute
    {
        public CozyGraphEntryKind Kind { get; }
        public string Name { get; set; }
        public string NameOptionName { get; set; }
        public string FlowOutputName { get; set; } = "Execute";

        public CozyGraphEntryAttribute(CozyGraphEntryKind kind)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// Lets a generated editor adapter replace one pre-release editor node type
    /// without retaining that old implementation in the package.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CozyNodeMovedFromAttribute : Attribute
    {
        public string SourceNamespace { get; }
        public string SourceAssembly { get; }
        public string SourceClassName { get; }

        public CozyNodeMovedFromAttribute(
            string sourceNamespace,
            string sourceAssembly,
            string sourceClassName)
        {
            SourceNamespace = sourceNamespace;
            SourceAssembly = sourceAssembly;
            SourceClassName = sourceClassName;
        }
    }
}
