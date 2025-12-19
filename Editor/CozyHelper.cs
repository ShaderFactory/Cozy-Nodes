using UnityEngine;
using Unity.GraphToolkit.Editor;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    public static class CozyHelper
    {
        public static object GetValue(IPort port)
        {
            if (port.isConnected)
            {
                if (port.firstConnectedPort.GetNode() is RuntimeCozyNode)
                {
                    Debug.Log("Getting value from ICozyValueNode");
                }
                else
                {
                    Debug.LogWarning("Can't get value because the connected" +
                        "node is not a ICozyValueNode.");
                }
                return null;
            }
            else
            {
                port.TryGetValue<object>(out object result);
                return result;
            }
        }
    }
}