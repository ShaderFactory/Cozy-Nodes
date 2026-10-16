using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// An imported place where a graph can begin execution. The entry stores the
    /// connected flow target rather than becoming a runtime node itself.
    /// </summary>
    [Serializable]
    public class RuntimeCozyGraphEntry
    {
        public CozyGraphEntryKind Kind;
        public string Name;
        public string TargetNodeID;
        public string TargetInputPortName;
    }
}
