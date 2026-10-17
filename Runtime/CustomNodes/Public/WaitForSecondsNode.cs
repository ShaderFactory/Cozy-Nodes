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
    [CozyNodeColor(0.114f, 0.122f, 0.141f)]
    [CozyNodeAccentColor(0.361f, 0.388f, 0.439f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/WaitForSeconds.png")]
    [FlowInput]
    [FlowOutput]
    public sealed class WaitForSecondsNode : CozyNode
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
