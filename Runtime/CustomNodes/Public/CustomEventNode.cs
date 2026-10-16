namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// A named graph entry that game code starts through CozyManager. It is an
    /// independent beginning of a graph flow, not a node that waits mid-flow.
    /// </summary>
    [CozyNodeMenu("Events", "Custom Event")]
    [CozyNodeColor(0.55f, 0.35f, 0.82f)]
    [CozyGraphEntry(
        CozyGraphEntryKind.CustomEvent,
        NameOptionName = nameof(EventName))]
    [FlowOutput("Execute")]
    public class CustomEventNode : CozyNode
    {
        [Option(
            "Event Name",
            Tooltip = "The name game code passes to CozyManager.RunCustomEvent.")]
        public string EventName = "New Event";
    }
}
