namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Implemented by a runtime node that pauses until it accepts a named trigger.
    /// </summary>
    public interface ICozyTriggerListener
    {
        bool TryReceiveTrigger(string triggerName, CozyManager cozyManager);
    }
}
