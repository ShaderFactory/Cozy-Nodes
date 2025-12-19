using GluonGui.Dialog;
using ShaderFactory.CozyGraphToolkit.Runtime;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using static Unity.GraphToolkit.Editor.Node;


namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    public class PrintNode : CozyEditorNode
    {




        protected override void OnDefinePorts(IPortDefinitionContext c)
        {
            c.AddInputPort("in").Build();
            c.AddOutputPort("out").Build();
            c.AddInputPort<string>("Message").Build();
        }
    }
}