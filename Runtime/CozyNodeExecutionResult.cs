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
            EnterSubgraph,
            ReturnFromSubgraph,
            Waiting,
            Finished,
            Failed
        }

        public State ExecutionState;
        public string OutputPortName;
        public string ErrorMessage;

        // EnterSubgraph uses these values to select the child frame's first node.
        // They are runtime instructions only and are never stored in a graph asset.
        public RuntimeCozyGraph Subgraph;
        public string TargetNodeID;
        public string TargetInputPortName;

        /// <summary>
        /// A non-negative value makes a Waiting result a timed pause. The runner
        /// owns this delay and follows <see cref="OutputPortName"/> when it
        /// expires; ordinary Wait results keep this at -1 and require an
        /// external trigger to resume.
        /// </summary>
        public float WaitDurationSeconds = -1f;

        public bool IsTimedWait =>
            ExecutionState == State.Waiting && WaitDurationSeconds >= 0f;

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

        public static CozyNodeExecutionResult EnterSubgraph(
            RuntimeCozyGraph subgraph,
            string targetNodeID,
            string targetInputPortName)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.EnterSubgraph,
                Subgraph = subgraph,
                TargetNodeID = targetNodeID,
                TargetInputPortName = targetInputPortName
            };
        }

        public static CozyNodeExecutionResult ReturnFromSubgraph(string outputPortName)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.ReturnFromSubgraph,
                OutputPortName = outputPortName
            };
        }

        public static CozyNodeExecutionResult Wait()
        {
            return new CozyNodeExecutionResult { ExecutionState = State.Waiting };
        }

        /// <summary>
        /// Pauses the current flow for a scaled game-time duration, then follows
        /// the named output without executing the waiting node a second time.
        /// </summary>
        public static CozyNodeExecutionResult WaitForSeconds(
            float seconds,
            string outputPortName)
        {
            return new CozyNodeExecutionResult
            {
                ExecutionState = State.Waiting,
                WaitDurationSeconds = seconds,
                OutputPortName = outputPortName
            };
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
