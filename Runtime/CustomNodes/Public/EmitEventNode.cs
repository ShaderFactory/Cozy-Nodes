namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Publishes a named event and optional typed payload to listeners on the
    /// running CozyManager, then continues its current flow.
    /// </summary>
    [CozyNodeMenu("Events", "Emit Event")]
    [CozyTypeParameter("T", "Payload Type", AllowArrayTypes = true)]
    [FlowInput]
    [FlowOutput]
    public sealed class EmitEventNode : CozyEventNode
    {
        [Input("Event Name", Tooltip = "The event name sent to CozyManager listeners.")]
        public string EventName;

        [Input("Payload", Tooltip = "Optional data sent with this event.")]
        [CozyTypeParameterPort("T")]
        public object Payload;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            if (context.Manager == null)
                return CozyNodeExecutionResult.Fail("Emit Event needs a CozyManager to run.");

            string eventName = context.GetInputValue<string>(nameof(EventName));
            if (string.IsNullOrWhiteSpace(eventName))
                return CozyNodeExecutionResult.Fail("Emit Event needs an Event Name.");

            context.Manager.InvokeEvent(new CozyEvent(
                eventName,
                context.GetInputValue<object>(nameof(Payload))));
            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
