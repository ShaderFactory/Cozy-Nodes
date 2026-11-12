namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Starts its connected flow once per Unity Update while this graph has no
    /// other active flow. It is intended for short per-frame graph work.
    /// </summary>
    [CozyNodeMenu("Events", "On Update", Tooltip = "Starts a flow every frame while this graph has no other active flow. Keep this work short.")]
    [CozyGraphEntry(CozyGraphEntryKind.OnUpdate, Name = "On Update")]
    [FlowOutput("Execute")]
    public sealed class OnUpdateNode : CozyEventNode
    {
    }
}
