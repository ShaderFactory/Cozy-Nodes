using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runtime behavior for InvokeEventNode. It publishes a named event through the
    /// running CozyManager, then continues through the node's normal flow output.
    /// </summary>
    [Serializable]
    public class InvokeEventRuntime : RuntimeCozyNode
    {
        public override CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            if (cozyManager == null)
                return CozyNodeExecutionResult.Fail("Invoke Event needs a CozyManager to run.");

            string eventName = GetInputValue("Event Name", cozyManager) as string;
            if (string.IsNullOrWhiteSpace(eventName))
                return CozyNodeExecutionResult.Fail("Invoke Event needs an Event Name.");

            // Payload can be any value the regular port resolver understands. Its
            // concrete type stays intact for the game code that receives the event.
            object payload = GetInputValue("Payload", cozyManager);
            CozyEvent cozyEvent = new CozyEvent(eventName, payload);
            cozyManager.InvokeEvent(cozyEvent);

            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
