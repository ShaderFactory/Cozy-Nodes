namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Starts its connected flow once per Unity LateUpdate while this graph has
    /// no other active flow. Use it when work must run after normal Updates.
    /// </summary>
    [CozyNodeMenu("Events", "On Late Update", Tooltip = "Starts a flow every LateUpdate while this graph has no other active flow.")]
    [CozyGraphEntry(CozyGraphEntryKind.OnLateUpdate, Name = "On Late Update")]
    [FlowOutput("Execute")]
    public sealed class OnLateUpdateNode : CozyEventNode
    {
    }
}
