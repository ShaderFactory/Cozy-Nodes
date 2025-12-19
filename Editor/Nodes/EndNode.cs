using System;
using Unity.GraphToolkit.Editor;


[Serializable]
public class EndNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("Execute").Build();
    }
}