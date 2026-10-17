namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Reads an element from a T[] array without throwing for an invalid index.
    /// Is Valid tells the graph whether Item contains an element from the array.
    /// </summary>
    [CozyNodeMenu("Utilities/Array", "Array Get Item")]
    public sealed class ArrayGetItemNode : CozyArrayNode
    {
        [Input("Array")]
        [CozyTypeParameterPort("T", IsArray = true)]
        public object Array;

        [Input]
        public int Index;

        [Output]
        [CozyTypeParameterPort("T")]
        public object Item;

        [Output("Is Valid")]
        public bool IsValid;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            System.Array values = context.GetInputValue<object>(nameof(Array)) as System.Array;
            int index = context.GetInputValue<int>(nameof(Index));
            bool isValid = values != null && index >= 0 && index < values.Length;

            if (outputPortName == nameof(IsValid))
                return isValid;

            if (outputPortName == nameof(Item))
                return isValid ? values.GetValue(index) : null;

            return null;
        }
    }
}
