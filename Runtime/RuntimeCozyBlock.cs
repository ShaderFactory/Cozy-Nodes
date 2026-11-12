using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Imported data for one block owned by a RuntimeCozyContextNode. Its owner
    /// runs the block in displayed order and carries any named branch out to
    /// the surrounding graph.
    /// </summary>
    [Serializable]
    internal sealed class RuntimeCozyBlock : RuntimeCozyAuthoredElement
    {
        public override object GetValue(CozyRuntimePort port, CozyGraphSession session)
        {
            if (port == null)
                return null;

            // Input values are stored in the imported graph. Only authored
            // outputs are calculated by the game block itself.
            if (!OutputPorts.Contains(port))
                return base.GetValue(port, session);

            CozyBlock block = GetBlock(session);
            if (block == null)
                return base.GetValue(port, session);

            CozyBlockContext context = new CozyBlockContext(this, session);
            return CozyValueTypeRegistry.CloneForRuntime(
                block.GetValue(context, port.Key));
        }

        public CozyBlockExecutionResult Execute(CozyGraphSession session)
        {
            CozyBlock block = GetBlock(session);
            if (block == null)
            {
                return CozyBlockExecutionResult.Fail(
                    $"Could not create custom Cozy block '{AuthoredTypeName}'.");
            }

            return block.Execute(new CozyBlockContext(this, session));
        }

        private CozyBlock GetBlock(CozyGraphSession session)
        {
            if (session == null)
                return null;

            Type blockType = CozyNodeReflection.FindBlockType(AuthoredTypeName);
            if (blockType == null)
                return null;

            return session.GetOrCreateBlock(this, blockType);
        }
    }
}
