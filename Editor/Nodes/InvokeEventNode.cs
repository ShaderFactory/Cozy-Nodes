using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Notifies the game code that a named event happened in the graph.
    /// The game subscribes to CozyManager.EventInvoked and decides how to react.
    /// </summary>
    [Serializable]
    [UseWithGraph(typeof(CozyGraph))]
    [Node("Cozy Nodes/Events & Triggers", null, "Invoke Event")]
    public class InvokeEventNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            InvokeEventRuntime runtimeInvokeEvent = new InvokeEventRuntime();
            runtimeInvokeEvent.NodeID = nodeID;
            runtimeInvokeEvent.NodeType = nodeType;
            runtimeInvokeEvent.Graph = graph;
            return runtimeInvokeEvent;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("in").Build();
            context.AddInputPort<string>("Event Name").Build();
            context.AddInputPort<object>("Payload").Build();
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
