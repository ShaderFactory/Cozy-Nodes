namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Selects the calculation performed by <see cref="FloatMathNode"/>.
    /// </summary>
    public enum FloatMathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>
    /// A core value node that performs one selected calculation on two floating
    /// point inputs. The selected operation is stored in the node's Options
    /// section, while A and B can each be direct values or connected outputs.
    /// </summary>
    [CozyNodeMenu("Utilities/Math", "Float Math")]
    public class FloatMathNode : CozyNode
    {
        [Option("Operation", Tooltip = "The calculation applied to A and B.")]
        public FloatMathOperation Operation = FloatMathOperation.Add;

        [Input]
        public float A;

        [Input]
        public float B;

        [Output]
        public float Result;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(Result))
                return null;

            float firstValue = context.GetInputValue<float>(nameof(A));
            float secondValue = context.GetInputValue<float>(nameof(B));
            FloatMathOperation operation =
                context.GetOptionValue<FloatMathOperation>(nameof(Operation));

            switch (operation)
            {
                case FloatMathOperation.Add:
                    return firstValue + secondValue;

                case FloatMathOperation.Subtract:
                    return firstValue - secondValue;

                case FloatMathOperation.Multiply:
                    return firstValue * secondValue;

                case FloatMathOperation.Divide:
                    return firstValue / secondValue;

                default:
                    return firstValue + secondValue;
            }
        }
    }
}
