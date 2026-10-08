using System;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
[UseWithGraph(typeof(CozyGraph))]
[Node("Cozy Nodes/Control", null, "Start Node")]
    public class StartNode : CozyEditorNode
    {
        public object EvaluateEditor()
        {
            throw new NotImplementedException();
        }

        public object GetOutputPortValue()
        {
            throw new NotImplementedException();
        }

        public object GetValue(IPort port)
        {
            throw new NotImplementedException();
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddOutputPort("Execute").Build();
            

        }

        public override bool IsFlowOutputPort(string portName)
        {
            return portName == "Execute";
        }
    }
}
