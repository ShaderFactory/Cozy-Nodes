namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Runs its Body once for each integer from Start through the value before
    /// End, advancing by Step. End is exclusive, matching a normal C# for loop.
    /// </summary>
    [CozyNodeMenu("Control Flow", "For Loop")]
    [CozyNodeColor(0.7f, 0.7f, 0.7f)]
    [CozyNodeAccentColor(0.737f, 0.706f, 0.624f)]
    [CozyNodeHeaderTextColor(0f, 0f, 0f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/ForLoop.png")]
    [FlowInput]
    [FlowInput("Break")]
    [FlowOutput("Body")]
    [FlowOutput("Exit")]
    public class ForNode : CozyNode
    {
        [Input("Start", Tooltip = "The first Index to run.")]
        public int Start;

        [Input("End", Tooltip = "The exclusive ending Index.")]
        public int End = 10;

        [Input("Step", Tooltip = "How much Index changes after each Body execution.")]
        public int Step = 1;

        [Output("Index", Tooltip = "The current Index while Body is executing.")]
        public int Index;

        private bool waitingForBody;
        private int end;
        private int step;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            return outputPortName == nameof(Index) ? Index : null;
        }

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            if (context.EnteredFlowInputName == "Break")
                return CozyNodeExecutionResult.ExitScope("Exit");

            if (waitingForBody)
            {
                Index += step;
                waitingForBody = false;
            }
            else
            {
                Index = context.GetInputValue<int>(nameof(Start));
                end = context.GetInputValue<int>(nameof(End));
                step = context.GetInputValue<int>(nameof(Step));

                if (step == 0)
                    return CozyNodeExecutionResult.Fail("For requires Step to be non-zero.");
            }

            if (!IsInRange())
                return CozyNodeExecutionResult.ContinueWith("Exit");

            waitingForBody = true;
            return CozyNodeExecutionResult.Call("Body");
        }

        public override void CancelFlowScope()
        {
            waitingForBody = false;
        }

        private bool IsInRange()
        {
            return step > 0 ? Index < end : Index > end;
        }
    }
}
