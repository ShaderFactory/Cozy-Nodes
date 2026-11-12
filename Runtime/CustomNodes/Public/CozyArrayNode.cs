namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared type setting for the built-in Array node family. Concrete nodes
    /// provide their own ports and behavior, while this base guarantees that
    /// they all expose the same Element Type picker.
    /// </summary>
    [CozyTypeParameter("T", "Element Type")]
    public abstract class CozyArrayNode : CozyArrayFamilyNode
    {
    }
}
