using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Provides empty arrays for each value family built into Cozy Nodes.
    /// Besides being useful when a graph needs a typed empty value, these
    /// concrete ports let Graph Toolkit discover every supported array type for
    /// Blackboard variables and local-subgraph boundaries.
    /// </summary>
    [CozyNodeMenu("Utilities/Array", "Array Empty", Tooltip = "An empty array of each supported type: string, integer, float and boolean.")]
    public sealed class ArrayEmptyNode : CozyArrayFamilyNode
    {
        [Output("String Array")]
        public string[] StringArray;

        [Output("Integer Array")]
        public int[] IntegerArray;

        [Output("Float Array")]
        public float[] FloatArray;

        [Output("Boolean Array")]
        public bool[] BooleanArray;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            return outputPortName switch
            {
                nameof(StringArray) => Array.Empty<string>(),
                nameof(IntegerArray) => Array.Empty<int>(),
                nameof(FloatArray) => Array.Empty<float>(),
                nameof(BooleanArray) => Array.Empty<bool>(),
                _ => null
            };
        }
    }
}
