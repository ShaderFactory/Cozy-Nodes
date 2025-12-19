using ShaderFactory.CozyGraphToolkit.Runtime;
using UnityEngine;

public class CustomPrintRuntime : RuntimeCozyNode
{
    public string evaluatedMessage;

    public override object GetValue(CozyRuntimePort _port)
    {
        object result = _port.GetValue();
        if (result == null)
        {
            Debug.LogWarning("_port.GetValue() == null !");
        }
        return result;
    }

    public override object Run()
    {
        var evaluatedMessage = GetValue(GetPortByName("Message"));
        Debug.Log(evaluatedMessage);
        return null;    
    }
}