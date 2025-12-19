using System;
using Unity.GraphToolkit.Editor;


namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    public class BranchString : Node
    {
        public bool Predicate;   // <-- MUST be public 

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {
            // c.AddInputPort("in").Build();
            c.AddOutputPort<string>("out").Build();
            c.AddInputPort<bool>("Predicate").Build();
            c.AddInputPort<string>("True").Build();
            c.AddInputPort<string>("False").Build();
        }
    }
}