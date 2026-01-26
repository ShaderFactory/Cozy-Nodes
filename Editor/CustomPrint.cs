using ShaderFactory.CozyGraphToolkit.Runtime;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    public class CustomPrint : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string _nodeID, string _nodeType, RuntimeCozyGraph _graph)
        {
            var node = new CustomPrintRuntime();
            node.NodeID = _nodeID;
            node.NodeType = _nodeType;
            node.Graph = _graph;
            return node;
        }

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {
            c.AddInputPort("Execute");
            c.AddOutputPort("Execute");
            c.AddInputPort<string>("Message");
        }
    }
}
