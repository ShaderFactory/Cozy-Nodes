namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Implemented by a public CozyNode that can resume its current flow after
    /// <see cref="CozyManager.Trigger"/> receives a matching named signal.
    /// </summary>
    public interface ICozyNodeTriggerListener
    {
        /// <summary>
        /// Returns true when this node accepted the signal and is ready for the
        /// graph runner to execute it again.
        /// </summary>
        bool TryReceiveTrigger(string triggerName, CozyNodeContext context);
    }
}
