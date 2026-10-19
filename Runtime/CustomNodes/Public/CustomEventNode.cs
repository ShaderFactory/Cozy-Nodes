namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// A named graph entry that game code starts through CozyManager. It is an
    /// independent beginning of a graph flow, not a node that waits mid-flow.
    /// </summary>
    [CozyNodeMenu("Events", "On Event")]
    [CozyGraphEntry(
        CozyGraphEntryKind.CustomEvent,
        NameOptionName = nameof(EventName))]
    [FlowOutput("Execute")]
    public class CustomEventNode : CozyEventNode
    {
        [Option(
            "Event Name",
            Tooltip = "The name game code uses to start this event entry.")]
        public string EventName = "New Event";
    }
}
