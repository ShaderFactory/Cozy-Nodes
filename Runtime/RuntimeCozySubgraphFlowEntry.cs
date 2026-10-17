using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Maps one GTK parent flow-port identity and its child-facing display name
    /// to the first regular node reached inside an imported child graph.
    /// </summary>
    [Serializable]
    public sealed class RuntimeCozySubgraphFlowEntry
    {
        // GTK names the parent port with the source variable's stable ID.
        public string ParentInputPortName;
        public string FlowInputName;
        public string TargetNodeID;
        public string TargetInputPortName;
    }
}
