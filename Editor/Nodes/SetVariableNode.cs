using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Changes one Blackboard variable for the current CozyManager execution.
    /// Connect a Graph Toolkit variable node to Variable, then provide the value
    /// that should be stored through Value.
    /// </summary>
    [Serializable]
    [UseWithGraph(typeof(CozyGraph))]
    [Node("Cozy Nodes/Variables", null, "Set Variable")]
    public class SetVariableNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            SetVariableRuntime runtimeSetVariable = new SetVariableRuntime();
            runtimeSetVariable.NodeID = nodeID;
            runtimeSetVariable.NodeType = nodeType;
            runtimeSetVariable.Graph = graph;
            return runtimeSetVariable;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort("in").Build();

            // A variable node carries its stable Blackboard identifier through this
            // connection. Object keeps this core node generic across all data types.
            context.AddInputPort<object>("Variable").Build();
            context.AddInputPort<object>("Value").Build();

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
