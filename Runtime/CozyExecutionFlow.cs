using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// One independently scheduled path through a CozyGraphScope. Flow-local
    /// node, context and block instances keep mutable custom-node state from
    /// leaking between concurrent On Event handlers.
    /// </summary>
    internal sealed class CozyExecutionFlow
    {
        private readonly Dictionary<RuntimeCozyNode, CozyNode> customNodeInstances =
            new Dictionary<RuntimeCozyNode, CozyNode>();
        private readonly Dictionary<RuntimeCozyContextNode, CozyContext> contextInstances =
            new Dictionary<RuntimeCozyContextNode, CozyContext>();
        private readonly Dictionary<RuntimeCozyBlock, CozyBlock> blockInstances =
            new Dictionary<RuntimeCozyBlock, CozyBlock>();

        internal CozyExecutionFrame ActiveFrame { get; }
        internal string EntryKey { get; }
        internal bool IsSuspended { get; set; }

        internal CozyGraphScope Scope => ActiveFrame.Scope;

        internal CozyExecutionFlow(
            CozyGraphScope scope,
            RuntimeCozyNode entryNode,
            string entryInputPortName,
            string entryKey)
        {
            ActiveFrame = new CozyExecutionFrame(scope)
            {
                CurrentNode = entryNode,
                EnteredFlowInputName = string.IsNullOrWhiteSpace(entryInputPortName)
                    ? "in"
                    : entryInputPortName
            };
            EntryKey = entryKey;
        }

        internal CozyNode GetOrCreateCustomNode(RuntimeCozyNode runtimeNode, Type nodeType)
        {
            if (runtimeNode == null || nodeType == null)
                return null;

            if (customNodeInstances.TryGetValue(runtimeNode, out CozyNode existingNode))
                return existingNode;

            CozyNode newNode = CozyNodeReflection.CreateNodeInstance(nodeType);
            if (newNode != null)
                customNodeInstances.Add(runtimeNode, newNode);

            return newNode;
        }

        internal CozyContext GetOrCreateContext(
            RuntimeCozyContextNode runtimeContext,
            Type contextType)
        {
            if (runtimeContext == null || contextType == null)
                return null;

            if (contextInstances.TryGetValue(runtimeContext, out CozyContext existingContext))
                return existingContext;

            CozyContext newContext = CozyNodeReflection.CreateContextInstance(contextType);
            if (newContext != null)
                contextInstances.Add(runtimeContext, newContext);

            return newContext;
        }

        internal CozyBlock GetOrCreateBlock(RuntimeCozyBlock runtimeBlock, Type blockType)
        {
            if (runtimeBlock == null || blockType == null)
                return null;

            if (blockInstances.TryGetValue(runtimeBlock, out CozyBlock existingBlock))
                return existingBlock;

            CozyBlock newBlock = CozyNodeReflection.CreateBlockInstance(blockType);
            if (newBlock != null)
                blockInstances.Add(runtimeBlock, newBlock);

            return newBlock;
        }
    }
}
