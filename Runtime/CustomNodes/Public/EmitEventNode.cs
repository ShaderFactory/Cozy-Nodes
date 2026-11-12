namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Publishes a named event and optional typed payload to listeners on the
    /// running CozyGraphSession, then continues its current flow.
    /// </summary>
    [CozyNodeMenu("Events", "Emit Event", Tooltip = "Sends a named event, with an optional payload, to listeners of this graph, then continues.")]
    [CozyTypeParameter("T", "Payload Type", AllowArrayTypes = true)]
    [FlowInput]
    [FlowOutput]
    public sealed class EmitEventNode : CozyEventNode
    {
        [Input("Event Name", Tooltip = "The event name sent to CozyGraphSession listeners.")]
        public string EventName;

        [Input("Payload", Tooltip = "Optional data sent with this event.")]
        [CozyTypeParameterPort("T")]
        public object Payload;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            if (context.Session == null)
                return CozyNodeExecutionResult.Fail("Emit Event needs a CozyGraphSession to run.");

            string eventName = context.GetInputValue<string>(nameof(EventName));
            if (string.IsNullOrWhiteSpace(eventName))
                return CozyNodeExecutionResult.Fail("Emit Event needs an Event Name.");

            context.Session.InvokeEvent(new CozyEvent(
                eventName,
                context.GetInputValue<object>(nameof(Payload))));
            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
