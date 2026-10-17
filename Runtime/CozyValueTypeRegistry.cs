using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// The single authority for Cozy value types. It defines how each scalar
    /// value is stored, restored, defaulted, and copied; one-dimensional arrays
    /// automatically inherit that behavior from their element type.
    /// </summary>
    public static class CozyValueTypeRegistry
    {
        private static readonly Dictionary<Type, CozyScalarValueCodec> codecs =
            new Dictionary<Type, CozyScalarValueCodec>
            {
                { typeof(string), new StringCodec() },
                { typeof(int), new IntCodec() },
                { typeof(float), new FloatCodec() },
                { typeof(bool), new BoolCodec() }
            };

        private static readonly Dictionary<CozyTypeParameterSelection, Type>
            scalarTypeParameterTypes =
                new Dictionary<CozyTypeParameterSelection, Type>
                {
                    { CozyTypeParameterSelection.String, typeof(string) },
                    { CozyTypeParameterSelection.Int, typeof(int) },
                    { CozyTypeParameterSelection.Float, typeof(float) },
                    { CozyTypeParameterSelection.Bool, typeof(bool) }
                };

        private static readonly Dictionary<CozyValueTypeParameterSelection, Type>
            valueTypeParameterTypes =
                new Dictionary<CozyValueTypeParameterSelection, Type>
                {
                    { CozyValueTypeParameterSelection.String, typeof(string) },
                    { CozyValueTypeParameterSelection.Int, typeof(int) },
                    { CozyValueTypeParameterSelection.Float, typeof(float) },
                    { CozyValueTypeParameterSelection.Bool, typeof(bool) },
                    { CozyValueTypeParameterSelection.ArrayOfString, typeof(string[]) },
                    { CozyValueTypeParameterSelection.ArrayOfInt, typeof(int[]) },
                    { CozyValueTypeParameterSelection.ArrayOfFloat, typeof(float[]) },
                    { CozyValueTypeParameterSelection.ArrayOfBool, typeof(bool[]) }
                };

        /// <summary>
        /// Returns the native Graph Toolkit option type used to select a
        /// concrete type parameter. Parameters that can represent a complete
        /// graph value receive scalar and array choices; element parameters
        /// receive scalar choices only.
        /// </summary>
        public static Type GetTypeParameterSelectionOptionType(bool allowArrayTypes)
        {
            return allowArrayTypes
                ? typeof(CozyValueTypeParameterSelection)
                : typeof(CozyTypeParameterSelection);
        }

        /// <summary>
        /// Returns the default option value matching
        /// <see cref="GetTypeParameterSelectionOptionType"/>.
        /// </summary>
        public static object GetDefaultTypeParameterSelection(bool allowArrayTypes)
        {
            return allowArrayTypes
                ? CozyValueTypeParameterSelection.String
                : CozyTypeParameterSelection.String;
        }

        /// <summary>
        /// Resolves a picker selection to the graph value type it represents.
        /// Keeping this lookup beside serialization means the type picker and
        /// the runtime can never silently disagree about supported values.
        /// </summary>
        public static bool TryGetTypeParameterValueType(object selection, out Type valueType)
        {
            switch (selection)
            {
                case CozyTypeParameterSelection scalarSelection:
                    return scalarTypeParameterTypes.TryGetValue(
                        scalarSelection,
                        out valueType);
                case CozyValueTypeParameterSelection valueSelection:
                    return valueTypeParameterTypes.TryGetValue(
                        valueSelection,
                        out valueType);
                default:
                    valueType = null;
                    return false;
            }
        }

        /// <summary>
        /// Returns whether Cozy can persist and run this scalar type or a
        /// one-dimensional array whose element type it supports.
        /// </summary>
        public static bool IsSupported(Type valueType)
        {
            if (valueType == null)
                return false;

            if (codecs.ContainsKey(valueType))
                return true;

            return valueType.IsArray &&
                   valueType.GetArrayRank() == 1 &&
                   codecs.ContainsKey(valueType.GetElementType());
        }

        /// <summary>
        /// Creates the conventional local default for a supported type. This is
        /// shared by reflected generic ports, rather than each port family
        /// inventing its own defaulting rule.
        /// </summary>
        public static object CreateDefaultValue(Type valueType)
        {
            if (!IsSupported(valueType))
                return null;

            if (valueType.IsArray)
                return Array.CreateInstance(valueType.GetElementType(), 0);

            return codecs[valueType].CreateDefaultValue();
        }

        /// <summary>
        /// Stores a supported scalar or one-dimensional array in Unity-safe
        /// fields. The declared type remains authoritative when a value is null.
        /// </summary>
        internal static void Write(
            CozySerializedValue destination,
            object value,
            Type declaredType = null)
        {
            if (destination == null)
                return;

            Type valueType = declaredType ?? value?.GetType();
            destination.Clear();
            if (!IsSupported(valueType))
                return;

            if (!valueType.IsArray)
            {
                codecs[valueType].Write(destination, value);
                return;
            }

            if (value is not Array source)
                return;

            Type elementType = valueType.GetElementType();
            CozyScalarValueCodec elementCodec = codecs[elementType];
            foreach (object element in source)
            {
                CozySerializedScalarValue serializedElement =
                    new CozySerializedScalarValue();
                elementCodec.Write(serializedElement, element);
                destination.ArrayElements.Add(serializedElement);
            }
        }

        /// <summary>
        /// Restores a supported scalar or one-dimensional array from Unity-safe
        /// fields. Unsupported types return null instead of creating a partly
        /// valid runtime value.
        /// </summary>
        internal static object Read(CozySerializedValue source, Type valueType)
        {
            if (source == null || !IsSupported(valueType))
                return null;

            if (!valueType.IsArray)
                return codecs[valueType].Read(source);

            Type elementType = valueType.GetElementType();
            CozyScalarValueCodec elementCodec = codecs[elementType];
            Array result = Array.CreateInstance(elementType, source.ArrayElements.Count);
            for (int index = 0; index < source.ArrayElements.Count; index++)
                result.SetValue(elementCodec.Read(source.ArrayElements[index]), index);

            return result;
        }

        /// <summary>
        /// Gives runtime values value semantics. Arrays are copied so a node or
        /// Blackboard read cannot mutate another consumer's value.
        /// </summary>
        public static object CloneForRuntime(object value)
        {
            if (value is not Array source)
            {
                return value != null && codecs.TryGetValue(value.GetType(), out CozyScalarValueCodec codec)
                    ? codec.Clone(value)
                    : value;
            }

            if (source.Rank != 1)
                return value;

            Type elementType = source.GetType().GetElementType();
            Array copy = Array.CreateInstance(elementType, source.Length);
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private abstract class CozyScalarValueCodec
        {
            public abstract object CreateDefaultValue();
            public abstract void Write(CozySerializedScalarValue destination, object value);
            public abstract object Read(CozySerializedScalarValue source);

            public virtual object Clone(object value)
            {
                return value;
            }
        }

        private sealed class StringCodec : CozyScalarValueCodec
        {
            public override object CreateDefaultValue() => string.Empty;

            public override void Write(CozySerializedScalarValue destination, object value)
            {
                destination.StringValue = value as string;
            }

            public override object Read(CozySerializedScalarValue source)
            {
                return source.StringValue;
            }
        }

        private sealed class IntCodec : CozyScalarValueCodec
        {
            public override object CreateDefaultValue() => 0;

            public override void Write(CozySerializedScalarValue destination, object value)
            {
                destination.IntValue = value is int intValue ? intValue : 0;
            }

            public override object Read(CozySerializedScalarValue source)
            {
                return source.IntValue;
            }
        }

        private sealed class FloatCodec : CozyScalarValueCodec
        {
            public override object CreateDefaultValue() => 0f;

            public override void Write(CozySerializedScalarValue destination, object value)
            {
                destination.FloatValue = value is float floatValue ? floatValue : 0f;
            }

            public override object Read(CozySerializedScalarValue source)
            {
                return source.FloatValue;
            }
        }

        private sealed class BoolCodec : CozyScalarValueCodec
        {
            public override object CreateDefaultValue() => false;

            public override void Write(CozySerializedScalarValue destination, object value)
            {
                destination.BoolValue = value is bool boolValue && boolValue;
            }

            public override object Read(CozySerializedScalarValue source)
            {
                return source.BoolValue;
            }
        }
    }

    /// <summary>
    /// Compatibility facade for existing custom Cozy code. New package code uses
    /// <see cref="CozyValueTypeRegistry"/> directly.
    /// </summary>
    public static class CozyValueTypes
    {
        public static bool IsSupported(Type valueType)
        {
            return CozyValueTypeRegistry.IsSupported(valueType);
        }

        public static object CloneForRuntime(object value)
        {
            return CozyValueTypeRegistry.CloneForRuntime(value);
        }
    }
}
