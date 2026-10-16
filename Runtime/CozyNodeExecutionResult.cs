using System;

namespace ShaderFactory.CozyNodes.Runtime
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
            Call,
            ExitScope,
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

        /// <summary>
        /// Runs a child flow path and resumes this node when that path reaches an
        /// unconnected output. Loop nodes use this to execute one body iteration.
        /// </summary>
        public static CozyNodeExecutionResult Call(string outputPortName)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.Call,
                OutputPortName = outputPortName
            };
        }

        /// <summary>
        /// Leaves the active scoped flow and follows the named output. For uses
        /// this when it receives a Break input.
        /// </summary>
        public static CozyNodeExecutionResult ExitScope(string outputPortName)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.ExitScope,
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
