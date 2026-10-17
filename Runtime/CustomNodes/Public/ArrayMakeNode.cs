using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Creates a one-dimensional array from a configurable number of values.
    /// </summary>
    [CozyNodeMenu("Utilities/Array", "Array Make")]
    public sealed class ArrayMakeNode : CozyArrayNode
    {
        [Option("Element Count", Tooltip = "How many values this array contains.")]
        public int ElementCount = 2;

        [InputList(
            nameof(ElementCount),
            "Element",
            Tooltip = "An element in the array.",
            MinimumCount = 0,
            MaximumCount = 32)]
        [CozyTypeParameterPort("T")]
        public object[] Elements;

        [Output("Array")]
        [CozyTypeParameterPort("T", IsArray = true)]
        public object Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            Type arrayType = context.GetOutputValueType(nameof(Result));
            if (arrayType == null || !arrayType.IsArray || arrayType.GetArrayRank() != 1)
                return null;

            Type elementType = arrayType.GetElementType();
            List<object> elements = context.GetInputValues<object>(nameof(Elements));
            Array result = Array.CreateInstance(elementType, elements.Count);
            for (int index = 0; index < elements.Count; index++)
                result.SetValue(elements[index], index);

            return result;
        }
    }
}
