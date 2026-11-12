namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation base for graph entries, event emitters, and flows
    /// that wait for an event. Events use a red Unreal-inspired language and
    /// share one icon across the family.
    /// </summary>
    [CozyNodeColor(0.5019608f, 0.0392157f, 0.0470588f)]
    [CozyNodeAccentColor(0.9019608f, 0.2117647f, 0.1921569f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/EventIcon.png")]
    public abstract class CozyEventNode : CozyNode
    {
    }
}
