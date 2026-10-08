using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Shared integer-input reader for core numeric nodes. It keeps integer nodes
    /// consistent: invalid values are reported and safely treated as zero rather
    /// than being silently rounded or converted.
    /// </summary>
    public static class CozyIntegerValue
    {
        public static int ReadInput(RuntimeCozyNode node, string portName, CozyManager cozyManager)
        {
            object value = node.GetInputValue(portName, cozyManager);
            if (value is int integerValue)
                return integerValue;

            if (value != null)
            {
                Debug.LogWarning(
                    $"{node.NodeType} expected an int on '{portName}', but received {value.GetType().Name}.");
            }

            return 0;
        }
    }
}
