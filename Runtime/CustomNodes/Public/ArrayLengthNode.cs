namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Returns the number of elements in a selected T[] array.
    /// </summary>
    [CozyNodeMenu("Utilities/Array", "Array Length")]
    public sealed class ArrayLengthNode : CozyArrayNode
    {
        [Input("Array")]
        [CozyTypeParameterPort("T", IsArray = true)]
        public object Array;

        [Output("Length")]
        public int Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            return (context.GetInputValue<object>(nameof(Array)) as System.Array)?.Length ?? 0;
        }
    }
}
