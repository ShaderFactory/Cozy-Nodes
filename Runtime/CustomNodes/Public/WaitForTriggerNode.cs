using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Pauses the current graph flow until CozyManager receives a matching
    /// named event. Unlike On Event, this resumes an already-running
    /// path instead of starting a new graph entry.
    /// </summary>
    [CozyNodeMenu(
        "Control Flow",
        "Wait For Event",
        Tooltip = "Pauses this flow until a matching named event is triggered.")]
    [FlowInput]
    [FlowOutput]
    public sealed class WaitForTriggerNode : CozyEventNode, ICozyNodeTriggerListener
    {
        [Input("Event Name", Tooltip = "The event name that resumes this waiting flow.")]
        public string TriggerName;

        [NonSerialized] private bool receivedTrigger;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            if (!receivedTrigger)
                return CozyNodeExecutionResult.Wait();

            receivedTrigger = false;
            return CozyNodeExecutionResult.ContinueWith("out");
        }

        public bool TryReceiveTrigger(string triggerName, CozyNodeContext context)
        {
            string expectedTriggerName = context.GetInputValue<string>(nameof(TriggerName));
            if (string.IsNullOrWhiteSpace(expectedTriggerName) ||
                !string.Equals(expectedTriggerName, triggerName, StringComparison.Ordinal))
            {
                return false;
            }

            receivedTrigger = true;
            return true;
        }

        public override void CancelFlowScope()
        {
            receivedTrigger = false;
        }
    }
}
