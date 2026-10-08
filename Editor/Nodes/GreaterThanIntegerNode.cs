using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Compares two whole-number values and provides true when A is greater than B.
    /// Its Result can connect directly to a Branch Node's Condition input.
    /// </summary>
    [Serializable]
    [UseWithGraph(typeof(CozyGraph))]
    [Node("Cozy Nodes/Utilities/Math", null, "Greater Than Integer")]
    public class GreaterThanIntegerNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            GreaterThanIntegerRuntime runtimeComparison = new GreaterThanIntegerRuntime();
            runtimeComparison.NodeID = nodeID;
            runtimeComparison.NodeType = nodeType;
            runtimeComparison.Graph = graph;
            return runtimeComparison;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<int>("A").WithDefaultValue(0).Build();
            context.AddInputPort<int>("B").WithDefaultValue(0).Build();
            context.AddOutputPort<bool>("Result").Build();
        }
    }
}
