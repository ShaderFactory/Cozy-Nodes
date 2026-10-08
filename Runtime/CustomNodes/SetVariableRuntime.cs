using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Stores a new value in the current CozyManager's Blackboard copy. The
    /// imported graph asset remains unchanged, so separate Managers remain isolated.
    /// </summary>
    [Serializable]
    public class SetVariableRuntime : RuntimeCozyNode
    {
        public override CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            if (cozyManager == null || cozyManager.RuntimeVariables == null)
                return CozyNodeExecutionResult.Fail("Set Variable needs a running CozyManager.");

            CozyRuntimePort variablePort = GetPortByName("Variable");
            if (variablePort == null || variablePort.type != CozyRuntimePort.PortType.Variable)
            {
                return CozyNodeExecutionResult.Fail(
                    "Set Variable needs a Blackboard variable connected to its Variable input.");
            }

            if (variablePort.variableReference == null || string.IsNullOrEmpty(variablePort.variableReference.variableID))
                return CozyNodeExecutionResult.Fail("Set Variable received an invalid Blackboard variable reference.");

            object value = GetInputValue("Value", cozyManager);
            bool wasStored = cozyManager.RuntimeVariables.TrySetValue(
                variablePort.variableReference.variableID,
                value,
                out string errorMessage);

            if (!wasStored)
                return CozyNodeExecutionResult.Fail("Set Variable failed: " + errorMessage);

            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
