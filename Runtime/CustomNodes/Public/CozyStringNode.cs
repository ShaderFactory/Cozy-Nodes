namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation for the built-in String node family. Concrete nodes
    /// still declare their own menu title and behavior, while this base keeps
    /// their visual language consistent.
    /// </summary>
    [CozyNodeColor(0.7372549f, 0.4196078f, 0.0431373f)]
    [CozyNodeAccentColor(0.9725490f, 0.7450980f, 0.4745098f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    public abstract class CozyStringNode : CozyNode
    {
    }
}
