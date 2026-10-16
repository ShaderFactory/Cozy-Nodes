using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// The Console message categories that graph authors can intentionally write.
    /// </summary>
    public enum CozyLogType
    {
        Log = (int)LogType.Log,
        Warning = (int)LogType.Warning,
        Error = (int)LogType.Error
    }

    /// <summary>
    /// Writes a text value to the Unity Console and continues execution.
    /// </summary>
    [CozyNodeMenu("Debug", "Print")]
    [CozyNodeWidth(270)]
    [FlowInput]
    [FlowOutput]
    public class PrintNode : CozyNode
    {
        [Option("Log Type", Tooltip = "The Console severity used for this message.")]
        public CozyLogType LogType = CozyLogType.Log;

        [Input("Message", Tooltip = "The text written to the Unity Console.")]
        public string Message;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            CozyLogType logType = context.GetOptionValue<CozyLogType>(nameof(LogType));
            string message = context.GetInputValue<string>(nameof(Message)) ?? string.Empty;
            Debug.unityLogger.Log((LogType)logType, message);
            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
