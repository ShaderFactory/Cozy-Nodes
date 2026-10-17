namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared type setting for the built-in Array node family. Concrete nodes
    /// provide their own ports and behavior, while this base guarantees that
    /// they all expose the same Element Type picker.
    /// </summary>
    [CozyTypeParameter("T", "Element Type")]
    [CozyNodeColor(0.463f, 0.404f, 0.231f)]
    [CozyNodeAccentColor(0.737f, 0.706f, 0.624f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    public abstract class CozyArrayNode : CozyNode
    {
    }
}
