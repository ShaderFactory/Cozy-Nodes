using System;
using ShaderFactory.CozyNodes.Runtime;
using ShaderFactory.CozyNodes.Editor;
using Unity.GraphToolkit.Editor;


[Serializable]
[UseWithGraph(typeof(CozyGraph))]
[Node("Cozy Nodes/Control", null, "End Node")]
[CozyNodeColor(0.82f, 0.27f, 0.31f)]
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
