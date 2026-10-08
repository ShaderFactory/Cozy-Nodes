using ShaderFactory.CozyGraphToolkit.Runtime;
using System;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// A core value node that joins First and Second into Result.
    /// </summary>
    [Serializable]
    [UseWithGraph(typeof(CozyGraph))]
    [Node("Cozy Nodes/Utilities", null, "Combine String")]
    public class CombineString : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            CombineStringRuntime runtimeCombineString = new CombineStringRuntime();
            runtimeCombineString.NodeID = nodeID;
            runtimeCombineString.NodeType = nodeType;
            runtimeCombineString.Graph = graph;
            return runtimeCombineString;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<string>("First").Build();
            context.AddInputPort<string>("Second").Build();
            context.AddOutputPort<string>("Result").Build();
        }
    }
}
