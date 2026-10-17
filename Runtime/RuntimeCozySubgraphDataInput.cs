using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Connects one parent-facing subgraph input port to the stable Blackboard
    /// variable that represents it inside the child graph.
    /// </summary>
    [Serializable]
    public sealed class RuntimeCozySubgraphDataInput
    {
        // Graph Toolkit names the parent port with the child variable's stable ID.
        public string ParentPortName;
        public string ChildVariableID;
        public string ValueTypeName;
    }
}
