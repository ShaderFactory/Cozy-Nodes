namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// The graph entry CozyManager runs automatically after it has initialized
    /// this graph's variables and node instances.
    /// </summary>
    [CozyNodeMenu("Events", "On Start")]
    [CozyGraphEntry(CozyGraphEntryKind.OnStart, Name = "On Start")]
    [FlowOutput("Execute")]
    public class OnStartNode : CozyEventNode
    {
    }
}
