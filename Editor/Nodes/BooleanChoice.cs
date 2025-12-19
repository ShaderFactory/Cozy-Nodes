using System;
using Unity.GraphToolkit.Editor;

[Serializable]
public class BooleanChoice : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
        context.AddInputPort<bool>("Condition").Build();
        context.AddOutputPort("True").Build();
        context.AddOutputPort("False").Build();
    }
}