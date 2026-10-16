using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Serializable value imported from a Graph Toolkit node option. Options are
    /// local configuration for one node instance, unlike input ports which can
    /// receive values from other nodes.
    /// </summary>
    [Serializable]
    public class RuntimeCozyNodeOption
    {
        public string Key;
        public string ValueTypeName;
        public string StringValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
        public string EnumValueName;

        public void SetValue(object value, Type valueType)
        {
            ValueTypeName = valueType?.AssemblyQualifiedName;

            if (valueType != null && valueType.IsEnum)
            {
                if (value is Enum enumValue)
                {
                    EnumValueName = enumValue.ToString();
                    return;
                }

                if (value != null)
                {
                    EnumValueName = Enum.ToObject(valueType, value).ToString();
                    return;
                }
            }

            if (value is string stringValue)
            {
                StringValue = stringValue;
                return;
            }

            if (value is float floatValue)
            {
                FloatValue = floatValue;
                return;
            }

            if (value is int intValue)
            {
                IntValue = intValue;
                return;
            }

            if (value is bool boolValue)
            {
                BoolValue = boolValue;
                return;
            }
        }

        public object GetValue()
        {
            Type valueType = string.IsNullOrWhiteSpace(ValueTypeName)
                ? null
                : Type.GetType(ValueTypeName);

            if (valueType == typeof(string))
                return StringValue;

            if (valueType == typeof(float))
                return FloatValue;

            if (valueType == typeof(int))
                return IntValue;

            if (valueType == typeof(bool))
                return BoolValue;

            if (valueType != null && valueType.IsEnum &&
                !string.IsNullOrWhiteSpace(EnumValueName))
            {
                try
                {
                    return Enum.Parse(valueType, EnumValueName);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
