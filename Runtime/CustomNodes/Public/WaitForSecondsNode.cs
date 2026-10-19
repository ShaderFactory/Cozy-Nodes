using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Pauses this graph's current flow for a scaled game-time duration, then
    /// continues through its output. The runner owns the pause; this node does
    /// not start a coroutine or retain a timer of its own.
    /// </summary>
    [CozyNodeMenu(
        "Control Flow",
        "Wait For Seconds",
        Tooltip = "Pauses this graph flow for the chosen number of scaled game-time seconds.")]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/WaitForSeconds.png")]
    [FlowInput]
    [FlowOutput]
    public sealed class WaitForSecondsNode : CozyControlFlowNode
    {
        [Input("Seconds", Tooltip = "The scaled game-time duration to wait. Zero continues immediately.")]
        public float Seconds = 1f;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            float seconds = context.GetInputValue<float>(nameof(Seconds));
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                return CozyNodeExecutionResult.Fail("Wait For Seconds needs a finite duration.");

            if (seconds <= 0f)
                return CozyNodeExecutionResult.ContinueWith("out");

            return CozyNodeExecutionResult.WaitForSeconds(seconds, "out");
        }
    }
}
