using ShaderFactory.CozyGraphToolkit.Runtime;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    public class CozyEditorNode : Node
    {
        public List<CozyRuntimePort> runtimePorts;

        public virtual RuntimeCozyNode CreateRuntimeNode(string _nodeID, string _nodeType, RuntimeCozyGraph _graph)
        {
            return new RuntimeCozyNode(_nodeID, _nodeType, _graph);
        }

        /// <summary>
        /// Tells the importer whether an input is an execution-flow port.
        /// Flow ports move execution forward, so they are not imported as values.
        /// Value inputs keep returning false and are evaluated on demand at runtime.
        /// </summary>
        public virtual bool IsFlowInputPort(string portName)
        {
            return false;
        }

        /// <summary>
        /// Tells the importer whether an output is an execution-flow port.
        /// A flow output is saved as a named connection, such as "out", "True",
        /// or "False". Value outputs keep returning false.
        /// </summary>
        public virtual bool IsFlowOutputPort(string portName)
        {
            return false;
        }

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {

        }
    }
}
