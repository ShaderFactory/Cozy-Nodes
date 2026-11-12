namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation for every built-in array utility. This is separate
    /// from <see cref="CozyArrayNode"/> because not every array utility exposes
    /// an element-type picker.
    /// </summary>
    [CozyNodeColor(0.1882353f, 0.3333333f, 0.3764706f)]
    [CozyNodeAccentColor(0.4745098f, 0.7450980f, 0.7921569f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    public abstract class CozyArrayFamilyNode : CozyNode
    {
    }
}
