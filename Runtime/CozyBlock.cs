namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Base class for one game-authored operation inside a CozyContext. Blocks
    /// reuse Cozy's Input, Output, Option, FlowInput, and FlowOutput attributes.
    /// </summary>
    public abstract class CozyBlock : CozyGraphElement
    {
        /// <summary>
        /// Runs this block when its owner context reaches it. The default keeps
        /// authoring-only blocks harmless by continuing to the next block.
        /// Return ExitContext with one of this block's FlowOutput names to leave
        /// the context through that branch.
        /// </summary>
        public virtual CozyBlockExecutionResult Execute(CozyBlockContext context)
        {
            return CozyBlockExecutionResult.Continue();
        }

        /// <summary>
        /// Calculates one value output when another graph element requests it.
        /// </summary>
        public virtual object GetValue(CozyBlockContext context, string outputPortName)
        {
            return null;
        }
    }
}
