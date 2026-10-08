using System;
using System.Globalization;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Defines the small, explicit set of implicit conversions that Cozy Nodes
    /// permits between connected value ports. Both the Editor and runtime use this
    /// class so a connection that looks valid in a graph also works in Play Mode.
    /// </summary>
    public static class CozyValueConverter
    {
        /// <summary>
        /// Returns whether a source port type can feed a destination port type.
        /// This intentionally does not allow lossy conversions such as float to int.
        /// </summary>
        public static bool CanConvert(Type sourceType, Type destinationType)
        {
            if (sourceType == null || destinationType == null)
                return false;

            if (destinationType.IsAssignableFrom(sourceType))
                return true;

            if (sourceType == typeof(int))
                return destinationType == typeof(float) || destinationType == typeof(string);

            if (sourceType == typeof(float) || sourceType == typeof(bool))
                return destinationType == typeof(string);

            return false;
        }

        /// <summary>
        /// Converts one runtime value to the type expected by its destination port.
        /// A failed conversion returns false so the caller can report a useful error.
        /// </summary>
        public static bool TryConvert(object value, Type destinationType, out object convertedValue)
        {
            convertedValue = null;

            if (destinationType == null)
            {
                convertedValue = value;
                return true;
            }

            if (value == null)
                return !destinationType.IsValueType;

            Type sourceType = value.GetType();
            if (destinationType.IsAssignableFrom(sourceType))
            {
                convertedValue = value;
                return true;
            }

            if (value is int integerValue)
            {
                if (destinationType == typeof(float))
                {
                    convertedValue = (float)integerValue;
                    return true;
                }

                if (destinationType == typeof(string))
                {
                    convertedValue = integerValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }

            if (value is float floatValue && destinationType == typeof(string))
            {
                convertedValue = floatValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (value is bool booleanValue && destinationType == typeof(string))
            {
                convertedValue = booleanValue.ToString();
                return true;
            }

            return false;
        }
    }
}
