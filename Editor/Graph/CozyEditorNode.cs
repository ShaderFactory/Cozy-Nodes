using ShaderFactory.CozyGraphToolkit.Runtime;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    public class CozyEditorNode : Node
    {
        public List<CozyRuntimePort> runtimePorts;

        public virtual RuntimeCozyNode CreateRuntimeNode(string _nodeID, string _nodeType, RuntimeCozyGraph _graph)
        {
            return new RuntimeCozyNode(_nodeID, _nodeType, _graph);
        }

        public void CreateRuntimePorts()
        {
            /*
            // Convert all INPUT ports into RuntimeCozyPorts.
            foreach (IPort port in node.GetInputPorts())
            {
                r.RegisterPort(port.name, GetPortValue(port, nodesIdDictionary), false);
            }

            // Convert also all OUTPUT ports into RuntimeCozyPorts.
            foreach (IPort port in node.GetOutputPorts())
            {
                r.RegisterPort(port.name, GetPortValue(port, nodesIdDictionary), true);
            }
            */
        }

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {

        }
    }
}