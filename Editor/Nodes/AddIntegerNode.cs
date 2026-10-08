using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Adds two whole-number values. This is a value node, so it calculates Result
    /// only when another node requests that output.
    /// </summary>
    [Serializable]
    [Node("Cozy Nodes/Math", null, "Add Integer")]
    public class AddIntegerNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            AddIntegerRuntime runtimeAddInteger = new AddIntegerRuntime();
            runtimeAddInteger.NodeID = nodeID;
            runtimeAddInteger.NodeType = nodeType;
            runtimeAddInteger.Graph = graph;
            return runtimeAddInteger;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<int>("A").WithDefaultValue(0).Build();
            context.AddInputPort<int>("B").WithDefaultValue(0).Build();
            context.AddOutputPort<int>("Result").Build();
        }
    }
}
