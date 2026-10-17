using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Pauses the current graph flow until CozyManager receives a matching
    /// named trigger. Unlike a Custom Event, this resumes an already-running
    /// path instead of starting a new graph entry.
    /// </summary>
    [CozyNodeMenu(
        "Control Flow",
        "Wait For Trigger",
        Tooltip = "Pauses this flow until CozyManager.Trigger receives the matching name.")]
    [CozyNodeColor(0.7882353f, 0.5921568f, 0f)]
    [CozyNodeAccentColor(0.9764706f, 0.9450980f, 0.8509804f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/WaitForTrigger.png")]
    [FlowInput]
    [FlowOutput]
    public sealed class WaitForTriggerNode : CozyNode, ICozyNodeTriggerListener
    {
        [Input("Trigger Name", Tooltip = "The name that must be passed to CozyManager.Trigger.")]
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
