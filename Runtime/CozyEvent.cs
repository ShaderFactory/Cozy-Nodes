namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Runtime-only notification sent from a Cozy graph to subscribed game code.
    /// Payload retains the type supplied by the graph and is never serialized into
    /// the graph asset.
    /// </summary>
    public class CozyEvent
    {
        public string EventName { get; }
        public object Payload { get; }

        public CozyEvent(string eventName)
        {
            EventName = eventName;
        }

        public CozyEvent(string eventName, object payload)
        {
            EventName = eventName;
            Payload = payload;
        }
    }
}
