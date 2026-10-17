using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Unity-serializable representation of a Cozy value. Arrays hold scalar
    /// elements, matching Cozy Nodes' deliberate one-dimensional-array limit.
    /// </summary>
    [Serializable]
    public sealed class CozySerializedValue : CozySerializedScalarValue
    {
        public List<CozySerializedScalarValue> ArrayElements = new();

        public void SetValue(object value, Type declaredType = null)
        {
            CozyValueTypeRegistry.Write(this, value, declaredType);
        }

        public object GetValue(Type valueType)
        {
            return CozyValueTypeRegistry.Read(this, valueType);
        }

        internal void Clear()
        {
            ClearScalarValues();
            ArrayElements.Clear();
        }
    }

    /// <summary>
    /// A non-recursive element stored inside a serialized Cozy array. Keeping
    /// this separate from <see cref="CozySerializedValue"/> prevents Unity from
    /// treating the supported one-dimensional arrays as an arbitrary object graph.
    /// </summary>
    [Serializable]
    public class CozySerializedScalarValue
    {
        public string StringValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;

        internal void ClearScalarValues()
        {
            StringValue = null;
            FloatValue = 0f;
            IntValue = 0;
            BoolValue = false;
        }
    }
}
