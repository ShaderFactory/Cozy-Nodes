using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Represents one flow-output gateway inside an imported child graph.
    /// Reaching it resumes the caller through the gateway's matching output.
    /// </summary>
    [Serializable]
    internal sealed class RuntimeCozySubgraphExitNode : RuntimeCozyNode
    {
        public string ParentFlowOutputName;

        public override CozyNodeExecutionResult Run(
            CozyGraphSession session,
            string enteredFlowInputName)
        {
            return CozyNodeExecutionResult.ReturnFromSubgraph(ParentFlowOutputName);
        }
    }
}
