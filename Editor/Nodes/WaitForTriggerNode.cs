using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Pauses graph execution until CozyManager receives a matching trigger name.
    /// The name can be typed into the node or supplied by another value node.
    /// </summary>
    [Serializable]
    [Node("Cozy Nodes/Flow", null, "Wait For Trigger")]
    public class WaitForTriggerNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            WaitForTriggerRuntime runtimeWaitForTrigger = new WaitForTriggerRuntime();
            runtimeWaitForTrigger.NodeID = nodeID;
            runtimeWaitForTrigger.NodeType = nodeType;
            runtimeWaitForTrigger.Graph = graph;
            return runtimeWaitForTrigger;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("in").Build();
            context.AddInputPort<string>("Trigger Name").Build();
            context.AddOutputPort("out").Build();
        }

        public override bool IsFlowInputPort(string portName)
        {
            return portName == "in";
        }

        public override bool IsFlowOutputPort(string portName)
        {
            return portName == "out";
        }
    }
}
