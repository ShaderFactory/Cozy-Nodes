namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation for the built-in String node family. Concrete nodes
    /// still declare their own menu title and behavior, while this base keeps
    /// their visual language consistent.
    /// </summary>
    // String nodes retain the standard dark header while their accent matches
    // the richer orange used by String ports.
    [CozyNodeColor(0.171f, 0.183f, 0.2115f)]
    [CozyNodeAccentColor(0.7372549f, 0.4196078f, 0.0431373f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    public abstract class CozyStringNode : CozyNode
    {
    }
}
