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
    }
}
