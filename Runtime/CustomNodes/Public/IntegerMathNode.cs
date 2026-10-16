namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Selects the calculation performed by <see cref="IntegerMathNode"/>.
    /// </summary>
    public enum IntegerMathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>
    /// A core value node that performs one selected calculation on two integer
    /// inputs. The selected operation belongs to this node's Options section.
    /// </summary>
    [CozyNodeMenu("Utilities/Math", "Integer Math")]
    public class IntegerMathNode : CozyNode
    {
        [Option("Operation", Tooltip = "The calculation applied to A and B.")]
        public IntegerMathOperation Operation = IntegerMathOperation.Add;

        [Input]
        public int A;

        [Input]
        public int B;

        [Output]
        public int Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            int firstValue = context.GetInputValue<int>(nameof(A));
            int secondValue = context.GetInputValue<int>(nameof(B));
            IntegerMathOperation operation =
                context.GetOptionValue<IntegerMathOperation>(nameof(Operation));

            switch (operation)
            {
                case IntegerMathOperation.Add:
                    return firstValue + secondValue;

                case IntegerMathOperation.Subtract:
                    return firstValue - secondValue;

                case IntegerMathOperation.Multiply:
                    return firstValue * secondValue;

                case IntegerMathOperation.Divide:
                    return secondValue == 0 ? 0 : firstValue / secondValue;

                default:
                    return firstValue + secondValue;
            }
        }
    }
}
