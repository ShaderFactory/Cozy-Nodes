namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Explicitly terminates the active Cozy graph execution. This ends the
    /// whole run, including when called from a loop or local subgraph.
    /// </summary>
    [CozyNodeMenu(
        "Control Flow",
        "Stop Graph",
        Tooltip = "Immediately stops this graph run and cancels its active flow.")]
    [CozyFlowTerminal]
    [FlowInput]
    public sealed class StopGraphNode : CozyControlFlowNode
    {
        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            return CozyNodeExecutionResult.Finish();
        }
    }
}
