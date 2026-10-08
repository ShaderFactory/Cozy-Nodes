using System;
using ShaderFactory.CozyGraphToolkit.Editor;
using Unity.GraphToolkit.Editor;


[Serializable]
[Node("Cozy Nodes/Flow", null, "End Node")]
public class EndNode : CozyEditorNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("Execute").Build();
    }

    public override bool IsFlowInputPort(string portName)
    {
        return portName == "Execute";
    }
}
