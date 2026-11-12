namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation and behavior base for nodes that direct graph flow.
    /// Keeping the control-flow palette here makes that category read as one
    /// family without repeating visual attributes on every node.
    /// </summary>
    // A muted warm white keeps control flow distinct without overpowering the
    // canvas, while retaining enough contrast for dark iconography and labels.
    [CozyNodeColor(0.7058824f, 0.6901961f, 0.6470588f)]
    [CozyNodeAccentColor(0.9019608f, 0.8549020f, 0.6392157f)]
    [CozyNodeHeaderTextColor(0.1058824f, 0.1137255f, 0.1294118f)]
    public abstract class CozyControlFlowNode : CozyNode
    {
    }
}
