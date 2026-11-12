namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Implemented by a runtime node that pauses until it accepts a named trigger.
    /// </summary>
    internal interface ICozyTriggerListener
    {
        bool TryReceiveTrigger(string triggerName, CozyGraphSession session);
    }
}
