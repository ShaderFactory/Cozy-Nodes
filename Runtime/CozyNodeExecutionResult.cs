using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Describes what the graph runner should do after a flow node has run.
    /// </summary>
    [Serializable]
    public class CozyNodeExecutionResult
    {
        public enum State
        {
            Continue,
            Waiting,
            Finished,
            Failed
        }

        public State ExecutionState;
        public string OutputPortName;
        public string ErrorMessage;

        public static CozyNodeExecutionResult ContinueWith(string outputPortName)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.Continue,
                OutputPortName = outputPortName
            };
        }

        public static CozyNodeExecutionResult Wait()
        {
            return new CozyNodeExecutionResult { ExecutionState = State.Waiting };
        }

        public static CozyNodeExecutionResult Finish()
        {
            return new CozyNodeExecutionResult { ExecutionState = State.Finished };
        }

        public static CozyNodeExecutionResult Fail(string errorMessage)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.Failed,
                ErrorMessage = errorMessage
            };
        }
    }
}
