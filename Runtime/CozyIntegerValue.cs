using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared integer-input reader for core numeric nodes. It keeps integer nodes
    /// consistent: invalid values are reported and safely treated as zero rather
    /// than being silently rounded or converted.
    /// </summary>
    internal static class CozyIntegerValue
    {
        public static int ReadInput(RuntimeCozyNode node, string portName, CozyGraphSession session)
        {
            object value = node.GetInputValue(portName, session);
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
