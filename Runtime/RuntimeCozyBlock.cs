using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Imported data for one block owned by a RuntimeCozyContextNode. Its owner
    /// runs the block in displayed order and carries any named branch out to
    /// the surrounding graph.
    /// </summary>
    [Serializable]
    public sealed class RuntimeCozyBlock : RuntimeCozyAuthoredElement
    {
        public override object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null)
                return null;

            // Input values are stored in the imported graph. Only authored
            // outputs are calculated by the game block itself.
            if (!OutputPorts.Contains(port))
                return base.GetValue(port, cozyManager);

            CozyBlock block = GetBlock(cozyManager);
            if (block == null)
                return base.GetValue(port, cozyManager);

            CozyBlockContext context = new CozyBlockContext(this, cozyManager);
            return CozyValueTypeRegistry.CloneForRuntime(
                block.GetValue(context, port.Key));
        }

        public CozyBlockExecutionResult Execute(CozyManager cozyManager)
        {
            CozyBlock block = GetBlock(cozyManager);
            if (block == null)
            {
                return CozyBlockExecutionResult.Fail(
                    $"Could not create custom Cozy block '{AuthoredTypeName}'.");
            }

            return block.Execute(new CozyBlockContext(this, cozyManager));
        }

        private CozyBlock GetBlock(CozyManager cozyManager)
        {
            if (cozyManager == null)
                return null;

            Type blockType = CozyNodeReflection.FindBlockType(AuthoredTypeName);
            if (blockType == null)
                return null;

            return cozyManager.GetOrCreateBlock(this, blockType);
        }
    }
}
