using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Describes what an authored block does within its owning context. Blocks
    /// normally continue to the next block; a named exit follows that block's
    /// flow output out of the context.
    /// </summary>
    [Serializable]
    public sealed class CozyBlockExecutionResult
    {
        public enum State
        {
            Continue,
            ExitContext,
            FinishGraph,
            Failed
        }

        public State ExecutionState;
        public string OutputPortName;
        public string ErrorMessage;

        public static CozyBlockExecutionResult Continue()
        {
            return new CozyBlockExecutionResult { ExecutionState = State.Continue };
        }

        public static CozyBlockExecutionResult ExitContext(string outputPortName)
        {
            return new CozyBlockExecutionResult
            {
                ExecutionState = State.ExitContext,
                OutputPortName = outputPortName
            };
        }

        public static CozyBlockExecutionResult FinishGraph()
        {
            return new CozyBlockExecutionResult { ExecutionState = State.FinishGraph };
        }

        public static CozyBlockExecutionResult Fail(string errorMessage)
        {
            return new CozyBlockExecutionResult
            {
                ExecutionState = State.Failed,
                ErrorMessage = errorMessage
            };
        }
    }
}
