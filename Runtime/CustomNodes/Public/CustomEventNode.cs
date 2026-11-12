namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// A named graph entry that game code starts through CozyGraphSession. It is an
    /// independent beginning of a graph flow, not a node that waits mid-flow.
    /// </summary>
    [CozyNodeMenu("Events", "On Event", Tooltip = "Starts a new flow when an event with this name is triggered by game code or a global event emitter.")]
    [CozyGraphEntry(
        CozyGraphEntryKind.CustomEvent,
        NameOptionName = nameof(EventName))]
    [CozyNodeWidth(270)]
    [FlowOutput("Execute")]
    public class CustomEventNode : CozyEventNode
    {
        [Option(
            "Event Name",
            Tooltip = "The name game code uses to start this event entry.")]
        public string EventName = "New Event";
    }
}
