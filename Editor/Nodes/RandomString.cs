using ShaderFactory.CozyGraphToolkit.Editor;
using ShaderFactory.CozyGraphToolkit.Runtime;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;


[Serializable]
public class RandomString : CozyEditorNode
{
    List<string> stringList;

    public RuntimeCozyNode CreateRuntimeNode()
    {
        RuntimeCozyNode result = new RandomStringRuntime();
        return result;
    }

    protected override void OnDefinePorts(IPortDefinitionContext c)
    {
        c.AddOutputPort<string>("Value").Build();
        c.AddInputPort<string>("String 1").Build();
        c.AddInputPort<string>("String 2").Build();
        c.AddInputPort<string>("String 3").Build();
        c.AddInputPort<string>("String 4").Build();
        c.AddInputPort<string>("String 5").Build();
    }


}
