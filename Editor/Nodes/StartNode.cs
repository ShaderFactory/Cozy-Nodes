using ShaderFactory.CozyGraphToolkit.Runtime;
using System;
using Unity.GraphToolkit.Editor;
using Unity.VisualScripting;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    public class StartNode : Node
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
    }
}