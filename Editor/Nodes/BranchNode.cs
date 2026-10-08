using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Chooses one of two execution paths from a boolean value.
    /// This is a flow node: Condition is data, while True and False are flow outputs.
    /// </summary>
    [Serializable]
    [Node("Cozy Nodes/Flow", null, "Branch Node")]
    public class BranchNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            BranchRuntime runtimeBranch = new BranchRuntime();
            runtimeBranch.NodeID = nodeID;
            runtimeBranch.NodeType = nodeType;
            runtimeBranch.Graph = graph;
            return runtimeBranch;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("in").Build();
            context.AddInputPort<bool>("Condition").Build();
            context.AddOutputPort("True").Build();
            context.AddOutputPort("False").Build();
        }

        public override bool IsFlowInputPort(string portName)
        {
            return portName == "in";
        }

        public override bool IsFlowOutputPort(string portName)
        {
            return portName == "True" || portName == "False";
        }
    }
}
