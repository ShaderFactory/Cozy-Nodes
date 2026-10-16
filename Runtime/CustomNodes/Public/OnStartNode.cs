namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// The graph entry CozyManager runs automatically after it has initialized
    /// this graph's variables and node instances.
    /// </summary>
    [CozyNodeMenu("Events", "On Start")]
    [CozyNodeColor(0.7882353f, 0.5921568f, 0f)]
    [CozyNodeAccentColor(0.9764706f, 0.9450980f, 0.8509804f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/OnStartFlag.png")]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    [CozyGraphEntry(CozyGraphEntryKind.OnStart, Name = "On Start")]
    [CozyNodeMovedFrom(
        "ShaderFactory.CozyNodes.Editor",
        "shaderfactory.cozynodes.editor",
        "StartNode")]
    [FlowOutput("Execute")]
    public class OnStartNode : CozyNode
    {
    }
}
