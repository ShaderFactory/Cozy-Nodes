using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using UnityEngine;

[Serializable]
public class CustomPrintRuntime : RuntimeCozyNode
{
    public string evaluatedMessage;

    public override object GetValue(CozyRuntimePort _port, CozyManager cozyManager)
    {
        // RuntimeCozyNode resolves both direct values and connected values.
        object result = base.GetValue(_port, cozyManager);
        if (result == null)
        {
            Debug.LogWarning("_port.GetValue() == null !");
        }
        return result;
    }

    public override CozyNodeExecutionResult Run(CozyManager cozyManager)
    {
        // GetInputValue also follows a connection to another node when needed.
        object evaluatedMessage = GetInputValue("Message", cozyManager);
        Debug.Log(evaluatedMessage);

        // Print is a flow node. After printing, it follows its named execution output.
        return CozyNodeExecutionResult.ContinueWith("out");
    }
}
