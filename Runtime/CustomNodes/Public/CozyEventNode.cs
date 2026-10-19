namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation base for graph entries, event emitters, and flows
    /// that wait for an event. Events use the same yellow language as graph
    /// triggers and share one icon across the family.
    /// </summary>
    [CozyNodeColor(0.7882353f, 0.5921568f, 0f)]
    [CozyNodeAccentColor(0.9764706f, 0.9450980f, 0.8509804f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/EventIcon.png")]
    public abstract class CozyEventNode : CozyNode
    {
    }
}
