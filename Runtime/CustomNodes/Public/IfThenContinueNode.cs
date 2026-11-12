namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Runs the True or the False path depending on Condition, then continues
    /// through Then once that path has finished.
    ///
    /// Graph Toolkit inputs accept a single wire, so two branches of an
    /// If/Else cannot join again into one node. This node avoids the join: the
    /// chosen path runs as a nested step (like a For Loop body) and returns
    /// here when it reaches an unconnected output, even after waiting.
    /// Leave a path unconnected to skip it.
    /// </summary>
    [CozyNodeMenu(
        "Control Flow",
        "If Then Continue",
        Tooltip = "Runs True or False depending on Condition. When that path ends (an unconnected output), continues through Then.")]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/IfElse.png")]
    [FlowInput]
    [FlowOutput("True")]
    [FlowOutput("False")]
    [FlowOutput("Then")]
    public class IfThenContinueNode : CozyControlFlowNode
    {
        [Input(Tooltip = "Chooses which path runs before Then.")]
        public bool Condition;

        private bool waitingForPath;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            // Second run: the chosen path has returned to this node.
            if (waitingForPath)
            {
                waitingForPath = false;
                return CozyNodeExecutionResult.ContinueWith("Then");
            }

            bool conditionIsTrue = context.GetInputValue<bool>(nameof(Condition));
            waitingForPath = true;
            return CozyNodeExecutionResult.Call(conditionIsTrue ? "True" : "False");
        }

        public override void CancelFlowScope()
        {
            waitingForPath = false;
        }
    }
}
