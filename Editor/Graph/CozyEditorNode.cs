using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    public class CozyEditorNode : Node
    {

        public virtual RuntimeCozyNode CreateRuntimeNode(string _nodeID, string _nodeType, RuntimeCozyGraph _graph)
        {
            return new RuntimeCozyNode(_nodeID, _nodeType, _graph);
        }

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {

        }
    }
}