using System;
using System.Reflection;
using ShaderFactory.CozyNodes.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Editor
{
    /// <summary>
    /// Shared GTK presentation for generated CozyContext and CozyBlock adapters.
    /// Game-authored types remain in runtime assemblies; only these adapters
    /// reference Graph Toolkit.
    /// </summary>
    internal static class CozyReflectedElementUi
    {
        private static readonly MethodInfo TryGetOptionValueMethod = FindTryGetOptionValueMethod();

        internal static void ApplyPresentation(Node node, CozyNodeDefinition definition)
        {
            if (node == null || definition == null)
                return;

            node.Title = definition.Title;
            if (definition.HasAccentColor)
                node.DefaultColor = definition.AccentColor;
            else if (definition.HasColor)
                node.DefaultColor = definition.Color;
        }

        internal static void DefineOptions(
            Node.IOptionDefinitionContext context,
            CozyNodeDefinition definition)
        {
            if (definition == null)
                return;

            foreach (CozyOptionDefinition option in definition.Options)
            {
                context.AddOption(option.Name, option.ValueType)
                    .WithDisplayName(option.DisplayName)
                    .WithTooltip(option.Tooltip)
                    .WithDefaultValue(option.DefaultValue)
                    .Build();
            }
        }

        /// <summary>
        /// Copies option values out of GTK's editor model into a runtime-safe
        /// representation shared by authored nodes, contexts, and blocks.
        /// </summary>
        internal static void CopyOptions(
            Node editorNode,
            CozyNodeDefinition definition,
            RuntimeCozyAuthoredElement runtimeElement)
        {
            if (editorNode == null || definition == null || runtimeElement == null)
                return;

            foreach (CozyOptionDefinition optionDefinition in definition.Options)
            {
                INodeOption graphToolkitOption = editorNode.GetNodeOptionByName(
                    optionDefinition.Name);
                object optionValue = GetOptionValue(graphToolkitOption, optionDefinition);
                runtimeElement.RegisterOption(
                    optionDefinition.Name,
                    optionValue,
                    optionDefinition.ValueType);
            }
        }

        internal static object GetOptionValue(
            INodeOption graphToolkitOption,
            CozyOptionDefinition optionDefinition)
        {
            if (graphToolkitOption == null || TryGetOptionValueMethod == null)
                return optionDefinition.DefaultValue;

            object fallbackValue = optionDefinition.DefaultValue;
            if (fallbackValue == null && optionDefinition.ValueType.IsValueType)
                fallbackValue = Activator.CreateInstance(optionDefinition.ValueType);

            MethodInfo typedGetValue = TryGetOptionValueMethod.MakeGenericMethod(
                optionDefinition.ValueType);
            object[] arguments = { fallbackValue };
            bool wasRead = (bool)typedGetValue.Invoke(graphToolkitOption, arguments);
            return wasRead ? arguments[0] : optionDefinition.DefaultValue;
        }

        internal static void DefinePorts(
            Node.IPortDefinitionContext context,
            CozyNodeDefinition definition)
        {
            if (definition == null)
                return;

            foreach (string flowInputName in definition.FlowInputs)
            {
                context.AddInputPort(flowInputName)
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }

            foreach (CozyPortDefinition input in definition.Inputs)
                AddInputPort(context, input);

            foreach (string flowOutputName in definition.FlowOutputs)
            {
                context.AddOutputPort(flowOutputName)
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }

            foreach (CozyPortDefinition output in definition.Outputs)
                AddOutputPort(context, output);
        }

        private static void AddInputPort(Node.IPortDefinitionContext context, CozyPortDefinition definition)
        {
            if (!CozyNodeReflection.IsSupportedPortValueType(definition.ValueType))
            {
                Debug.LogError(
                    $"Custom Cozy graph element has unsupported input type " +
                    $"'{definition.ValueType?.Name}' on '{definition.Name}'.");
                return;
            }

            if (definition.ValueType == typeof(string) && definition.IsTextArea)
            {
                context.AddInputPort<string>(definition.Name)
                    .AsTextArea(definition.MinTextAreaLines, definition.MaxTextAreaLines)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .WithDefaultValue(definition.DefaultValue as string)
                    .Build();
                return;
            }

            ITypedInputPortBuilder builder = context.AddInputPort(definition.Name)
                .WithDataType(definition.ValueType)
                .WithDisplayName(definition.DisplayName)
                .WithTooltip(definition.Tooltip);

            if (!definition.ValueType.IsArray)
                builder.WithDefaultValue(definition.DefaultValue);

            builder.Build();
        }

        private static void AddOutputPort(Node.IPortDefinitionContext context, CozyPortDefinition definition)
        {
            if (!CozyNodeReflection.IsSupportedPortValueType(definition.ValueType))
            {
                Debug.LogError(
                    $"Custom Cozy graph element has unsupported output type " +
                    $"'{definition.ValueType?.Name}' on '{definition.Name}'.");
                return;
            }

            context.AddOutputPort(definition.Name)
                .WithDataType(definition.ValueType)
                .WithDisplayName(definition.DisplayName)
                .WithTooltip(definition.Tooltip)
                .Build();
        }

        private static MethodInfo FindTryGetOptionValueMethod()
        {
            foreach (MethodInfo method in typeof(INodeOption).GetMethods())
            {
                if (method.Name == nameof(INodeOption.TryGetValue) &&
                    method.IsGenericMethodDefinition)
                {
                    return method;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Editor-only GTK model generated for a game-authored CozyContext.
    /// </summary>
    [Serializable]
    public abstract class CustomCozyContext : ContextNode
    {
        [SerializeField] private string customContextTypeName;

        protected virtual Type GetCustomContextType()
        {
            return null;
        }

        public override void OnEnable()
        {
            Type generatedContextType = GetCustomContextType();
            if (generatedContextType != null)
                customContextTypeName = generatedContextType.AssemblyQualifiedName;

            base.OnEnable();
            CozyReflectedElementUi.ApplyPresentation(this, GetDefinition());
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            CozyReflectedElementUi.DefineOptions(context, GetDefinition());
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            CozyReflectedElementUi.DefinePorts(context, GetDefinition());
        }

        internal Type ResolveCustomContextType()
        {
            Type generatedContextType = GetCustomContextType();
            if (generatedContextType != null)
                customContextTypeName = generatedContextType.AssemblyQualifiedName;

            return CozyNodeReflection.FindContextType(customContextTypeName);
        }

        internal RuntimeCozyContextNode CreateRuntimeNode(
            string nodeID,
            string nodeType,
            RuntimeCozyGraph graph)
        {
            RuntimeCozyContextNode runtimeNode = new RuntimeCozyContextNode();
            runtimeNode.NodeID = nodeID;
            runtimeNode.NodeType = nodeType;
            runtimeNode.Graph = graph;
            runtimeNode.AuthoredTypeName = customContextTypeName;
            CozyNodeDefinition definition = GetDefinition();
            runtimeNode.DisplayName = definition?.Title;
            CozyReflectedElementUi.CopyOptions(this, definition, runtimeNode);
            return runtimeNode;
        }

        private CozyNodeDefinition GetDefinition()
        {
            return CozyNodeReflection.GetDefinition(ResolveCustomContextType());
        }
    }

    /// <summary>
    /// Editor-only GTK model generated for a game-authored CozyBlock.
    /// </summary>
    [Serializable]
    public abstract class CustomCozyBlock : BlockNode
    {
        [SerializeField] private string customBlockTypeName;

        protected virtual Type GetCustomBlockType()
        {
            return null;
        }

        public override void OnEnable()
        {
            Type generatedBlockType = GetCustomBlockType();
            if (generatedBlockType != null)
                customBlockTypeName = generatedBlockType.AssemblyQualifiedName;

            base.OnEnable();
            CozyReflectedElementUi.ApplyPresentation(this, GetDefinition());
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            CozyReflectedElementUi.DefineOptions(context, GetDefinition());
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);
            CozyReflectedElementUi.DefinePorts(context, GetDefinition());
        }

        internal Type ResolveCustomBlockType()
        {
            Type generatedBlockType = GetCustomBlockType();
            if (generatedBlockType != null)
                customBlockTypeName = generatedBlockType.AssemblyQualifiedName;

            return CozyNodeReflection.FindBlockType(customBlockTypeName);
        }

        internal RuntimeCozyBlock CreateRuntimeBlock(
            string nodeID,
            string nodeType,
            RuntimeCozyGraph graph)
        {
            RuntimeCozyBlock runtimeBlock = new RuntimeCozyBlock();
            runtimeBlock.NodeID = nodeID;
            runtimeBlock.NodeType = nodeType;
            runtimeBlock.Graph = graph;
            runtimeBlock.AuthoredTypeName = customBlockTypeName;
            CozyNodeDefinition definition = GetDefinition();
            runtimeBlock.DisplayName = definition?.Title;
            CozyReflectedElementUi.CopyOptions(this, definition, runtimeBlock);
            return runtimeBlock;
        }

        private CozyNodeDefinition GetDefinition()
        {
            return CozyNodeReflection.GetDefinition(ResolveCustomBlockType());
        }
    }
}
