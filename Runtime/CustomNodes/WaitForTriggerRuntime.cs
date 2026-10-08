using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runtime behavior for WaitForTriggerNode. It remains paused until the
    /// CozyManager receives a trigger with the same name configured on this node.
    /// </summary>
    [Serializable]
    public class WaitForTriggerRuntime : RuntimeCozyNode, ICozyTriggerListener
    {
        // This flag belongs only to the current game session. The graph asset keeps
        // the node definition, not whether a previous play session received a trigger.
        [NonSerialized] private bool receivedTrigger;

        public override CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            if (receivedTrigger)
            {
                receivedTrigger = false;
                return CozyNodeExecutionResult.ContinueWith("out");
            }

            return CozyNodeExecutionResult.Wait();
        }

        public bool TryReceiveTrigger(string triggerName, CozyManager cozyManager)
        {
            string expectedTriggerName = GetInputValue("Trigger Name", cozyManager) as string;

            // A blank name is never a useful event identifier. It also prevents an
            // accidental empty UI Button event from resuming this node.
            if (string.IsNullOrWhiteSpace(expectedTriggerName))
                return false;

            if (!string.Equals(expectedTriggerName, triggerName, StringComparison.Ordinal))
                return false;

            receivedTrigger = true;
            return true;
        }
    }
}
