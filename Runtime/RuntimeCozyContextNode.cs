using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Imported representation of one authored CozyContext. Its blocks stay
    /// owned by the context so they cannot accidentally run as independent
    /// graph nodes.
    /// </summary>
    [Serializable]
    public sealed class RuntimeCozyContextNode : RuntimeCozyAuthoredElement
    {
        public List<RuntimeCozyBlock> Blocks = new List<RuntimeCozyBlock>();

        public bool TryGetBlock(string blockID, out RuntimeCozyBlock block)
        {
            foreach (RuntimeCozyBlock candidate in Blocks)
            {
                if (candidate != null && candidate.NodeID == blockID)
                {
                    block = candidate;
                    return true;
                }
            }

            block = null;
            return false;
        }

        /// <summary>
        /// Returns the internal context-level flow key used to carry a named
        /// block branch into the surrounding graph. It is deliberately derived
        /// from both IDs so identical output labels on two blocks never collide.
        /// </summary>
        public static string GetBlockFlowOutputKey(string blockID, string outputPortName)
        {
            return blockID + "::" + outputPortName;
        }

        public override CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            CozyContext context = GetContext(cozyManager);
            if (context == null)
            {
                return CozyNodeExecutionResult.Fail(
                    $"Could not create custom Cozy context '{AuthoredTypeName}'.");
            }

            CozyContextContext contextContext = new CozyContextContext(this, cozyManager);
            context.OnEnter(contextContext);

            foreach (RuntimeCozyBlock block in Blocks)
            {
                if (block == null)
                    continue;

                CozyBlockExecutionResult blockResult = block.Execute(cozyManager);
                if (blockResult == null)
                {
                    return CozyNodeExecutionResult.Fail(
                        $"Block '{block.DisplayName}' returned no execution result.");
                }

                if (blockResult.ExecutionState == CozyBlockExecutionResult.State.Continue)
                    continue;

                context.OnExit(contextContext);

                if (blockResult.ExecutionState == CozyBlockExecutionResult.State.ExitContext)
                {
                    if (string.IsNullOrWhiteSpace(blockResult.OutputPortName))
                    {
                        return CozyNodeExecutionResult.Fail(
                            $"Block '{block.DisplayName}' tried to leave its context without choosing a flow output.");
                    }

                    return CozyNodeExecutionResult.ContinueWith(
                        GetBlockFlowOutputKey(block.NodeID, blockResult.OutputPortName));
                }

                if (blockResult.ExecutionState == CozyBlockExecutionResult.State.FinishGraph)
                    return CozyNodeExecutionResult.Finish();

                return CozyNodeExecutionResult.Fail(blockResult.ErrorMessage);
            }

            context.OnExit(contextContext);
            return CozyNodeExecutionResult.ContinueWith("out");
        }

        private CozyContext GetContext(CozyManager cozyManager)
        {
            if (cozyManager == null)
                return null;

            Type contextType = CozyNodeReflection.FindContextType(AuthoredTypeName);
            if (contextType == null)
                return null;

            return cozyManager.GetOrCreateContext(this, contextType);
        }
    }
}
