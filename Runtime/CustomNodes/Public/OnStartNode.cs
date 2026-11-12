namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// The graph's default entry. A CozyGraphSession runs it whenever Start is
    /// called, whether that session comes from a CozyManager or game code.
    /// </summary>
    [CozyNodeMenu("Events", "On Graph Start", Tooltip = "Where the graph begins when it starts running.")]
    [CozyGraphEntry(CozyGraphEntryKind.OnStart, Name = "On Graph Start")]
    [FlowOutput("Execute")]
    public class OnStartNode : CozyEventNode
    {
    }
}
