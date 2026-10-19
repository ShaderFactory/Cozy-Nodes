using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Calls an imported local subgraph. Its flow output is chosen only when the
    /// child reaches a matching RuntimeCozySubgraphExitNode.
    /// </summary>
    [Serializable]
    public sealed class RuntimeCozySubgraphNode : RuntimeCozyNode
    {
        public RuntimeCozyGraph Subgraph;

        [SerializeReference]
        public List<RuntimeCozySubgraphFlowEntry> FlowEntries =
            new List<RuntimeCozySubgraphFlowEntry>();

        [SerializeReference]
        public List<RuntimeCozySubgraphDataInput> DataInputs =
            new List<RuntimeCozySubgraphDataInput>();

        [SerializeReference]
        public List<RuntimeCozySubgraphDataOutput> DataOutputs =
            new List<RuntimeCozySubgraphDataOutput>();

        /// <summary>
        /// Evaluates each parent-facing data input while the caller's execution
        /// frame is active, then writes the values into the fresh child variable
        /// scope. The binding is keyed by Graph Toolkit's stable variable ID, so
        /// renaming a child Blackboard entry does not break the call.
        /// </summary>
        public bool TryPopulateChildVariables(
            CozyManager cozyManager,
            CozyRuntimeVariables childVariables,
            out string errorMessage)
        {
            if (childVariables == null)
            {
                errorMessage = $"Subgraph '{NodeType}' has no child variable scope.";
                return false;
            }

            foreach (RuntimeCozySubgraphDataInput dataInput in DataInputs)
            {
                if (dataInput == null ||
                    string.IsNullOrEmpty(dataInput.ParentPortName) ||
                    string.IsNullOrEmpty(dataInput.ChildVariableID))
                {
                    errorMessage = $"Subgraph '{NodeType}' has an invalid data input binding.";
                    return false;
                }

                object value = GetInputValue(dataInput.ParentPortName, cozyManager);
                if (!childVariables.TrySetValue(
                        dataInput.ChildVariableID,
                        value,
                        out errorMessage))
                {
                    errorMessage =
                        $"Subgraph '{NodeType}' could not set input " +
                        $"'{dataInput.ParentPortName}': {errorMessage}";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Captures the child's declared output variables immediately before the
        /// caller resumes. The runner owns where those per-call values live;
        /// this node only knows the stable variable-to-port mapping.
        /// </summary>
        public bool TryCollectChildOutputValues(
            CozyManager cozyManager,
            CozyRuntimeVariables childVariables,
            out Dictionary<string, object> outputValues,
            out string errorMessage)
        {
            outputValues = new Dictionary<string, object>();
            if (childVariables == null)
            {
                errorMessage = $"Subgraph '{NodeType}' has no child variable scope.";
                return false;
            }

            foreach (RuntimeCozySubgraphDataOutput dataOutput in DataOutputs)
            {
                if (dataOutput == null ||
                    string.IsNullOrEmpty(dataOutput.ParentPortName) ||
                    string.IsNullOrEmpty(dataOutput.ChildVariableID))
                {
                    errorMessage = $"Subgraph '{NodeType}' has an invalid data output binding.";
                    return false;
                }

                if (!dataOutput.TryResolveValue(
                        Subgraph,
                        cozyManager,
                        childVariables,
                        out object value,
                        out string sourceError))
                {
                    errorMessage =
                        $"Subgraph '{NodeType}' could not resolve output " +
                        $"'{dataOutput.ParentPortName}': {sourceError}";
                    return false;
                }

                outputValues[dataOutput.ParentPortName] =
                    CozyValueTypeRegistry.CloneForRuntime(value);
            }

            errorMessage = null;
            return true;
        }

        public override object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null || !IsDataOutputPort(port.Key))
                return base.GetValue(port, cozyManager);

            if (cozyManager == null ||
                !cozyManager.TryGetSubgraphOutputValue(this, port.Key, out object value))
            {
                Debug.LogWarning(
                    $"Subgraph '{NodeType}' output '{port.Key}' was read before " +
                    "the subgraph reached a flow output.");
                return null;
            }

            if (CozyValueConverter.TryConvert(value, port.GetValueType(), out object convertedValue))
                return CozyValueTypeRegistry.CloneForRuntime(convertedValue);

            string sourceTypeName = value == null ? "null" : value.GetType().Name;
            string destinationTypeName = port.GetValueType()?.Name ?? "unknown";
            Debug.LogWarning(
                $"Subgraph '{NodeType}' output '{port.Key}' cannot convert " +
                $"{sourceTypeName} to {destinationTypeName}.");
            return null;
        }

        public override CozyNodeExecutionResult Run(
            CozyManager cozyManager,
            string enteredFlowInputName)
        {
            string flowInputName = string.IsNullOrWhiteSpace(enteredFlowInputName)
                ? "in"
                : enteredFlowInputName;

            if (Subgraph == null)
            {
                return CozyNodeExecutionResult.Fail(
                    $"Subgraph node '{NodeType}' has no imported child graph.");
            }

            foreach (RuntimeCozySubgraphFlowEntry flowEntry in FlowEntries)
            {
                if (flowEntry == null ||
                    (flowEntry.FlowInputName != flowInputName &&
                     flowEntry.ParentInputPortName != flowInputName))
                    continue;

                return CozyNodeExecutionResult.EnterSubgraph(
                    Subgraph,
                    flowEntry.TargetNodeID,
                    flowEntry.TargetInputPortName);
            }

            return CozyNodeExecutionResult.Fail(
                $"Subgraph '{NodeType}' has no flow input matching '{flowInputName}'.");
        }

        private bool IsDataOutputPort(string portName)
        {
            foreach (RuntimeCozySubgraphDataOutput dataOutput in DataOutputs)
            {
                if (dataOutput?.ParentPortName == portName)
                    return true;
            }

            return false;
        }
    }
}
