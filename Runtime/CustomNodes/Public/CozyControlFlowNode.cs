namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation and behavior base for nodes that direct graph flow.
    /// Keeping the control-flow palette here makes that category read as one
    /// family without repeating visual attributes on every node.
    /// </summary>
    [CozyNodeColor(0.7882353f, 0.5921568f, 0f)]
    [CozyNodeAccentColor(0.9764706f, 0.9450980f, 0.8509804f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    public abstract class CozyControlFlowNode : CozyNode
    {
    }
}
