using System;
using ShaderFactory.CozyGraphToolkit.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Graph Toolkit model used internally for every game-authored CozyNode.
    /// Game developers never derive from this editor-only class. The adapter
    /// generator creates a small concrete subclass for each CozyNode so Graph
    /// Toolkit can discover it through its normal Node library.
    /// </summary>
    [Serializable]
    public class CustomCozyNode : CozyEditorNode
    {
        [SerializeField] private string customNodeTypeName;

        public string CustomNodeTypeName => customNodeTypeName;

        /// <summary>
        /// Configures this adapter before it is added to a graph. Graph Toolkit
        /// defines the ports when AddNode is called, using this saved type name.
        /// Presentation is intentionally updated later, once the node belongs to
        /// a graph and Graph Toolkit has created its internal model.
        /// </summary>
        public void Configure(Type customNodeType)
        {
            customNodeTypeName = customNodeType?.AssemblyQualifiedName;
        }

        public override void OnEnable()
        {
            // Generated adapters identify their runtime type here. Keeping this
            // in the base class lets every generated class stay tiny and makes
            // direct values, flow ports, importing, and runtime execution share
            // exactly the same implementation.
            ConfigureGeneratedNodeType();
            base.OnEnable();
        }

        /// <summary>
        /// Generated adapters override this method with typeof(TheGameNode).
        /// A null result is valid for older serialized CustomCozyNode instances
        /// that already contain their saved type name.
        /// </summary>
        protected virtual Type GetCustomNodeType()
        {
            return null;
        }

        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            ReflectedCozyRuntimeNode runtimeNode = new ReflectedCozyRuntimeNode();
            runtimeNode.NodeID = nodeID;
            runtimeNode.NodeType = nodeType;
            runtimeNode.Graph = graph;
            runtimeNode.CustomNodeTypeName = customNodeTypeName;
            return runtimeNode;
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            CozyNodeDefinition definition = GetDefinition();
            if (definition == null)
                return;

            foreach (string flowInputName in definition.FlowInputs)
                context.AddInputPort(flowInputName).Build();

            foreach (CozyPortDefinition input in definition.Inputs)
                AddInputPort(context, input);

            foreach (CozyPortDefinition output in definition.Outputs)
                AddOutputPort(context, output);

            foreach (string flowOutputName in definition.FlowOutputs)
                context.AddOutputPort(flowOutputName).Build();
        }

        public override bool IsFlowInputPort(string portName)
        {
            CozyNodeDefinition definition = GetDefinition();
            return definition != null && definition.FlowInputs.Contains(portName);
        }

        public override bool IsFlowOutputPort(string portName)
        {
            CozyNodeDefinition definition = GetDefinition();
            return definition != null && definition.FlowOutputs.Contains(portName);
        }

        private CozyNodeDefinition GetDefinition()
        {
            ConfigureGeneratedNodeType();
            Type nodeType = CozyNodeReflection.FindNodeType(customNodeTypeName);
            return CozyNodeReflection.GetDefinition(nodeType);
        }

        private void ConfigureGeneratedNodeType()
        {
            if (!string.IsNullOrWhiteSpace(customNodeTypeName))
                return;

            Type generatedNodeType = GetCustomNodeType();
            if (generatedNodeType != null)
                Configure(generatedNodeType);
        }

        private void AddInputPort(IPortDefinitionContext context, CozyPortDefinition definition)
        {
            // CozyRuntimePort currently supports these primitive value types. Keep
            // this explicit so an unsupported game field fails clearly instead of
            // producing a graph connection that cannot be serialized at runtime.
            if (definition.ValueType == typeof(string))
            {
                if (definition.IsTextArea)
                {
                    context.AddInputPort<string>(definition.Name)
                        .AsTextArea(definition.MinTextAreaLines, definition.MaxTextAreaLines)
                        .WithDisplayName(definition.DisplayName)
                        .WithTooltip(definition.Tooltip)
                        .WithDefaultValue(definition.DefaultValue as string)
                        .Build();
                    return;
                }

                context.AddInputPort<string>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .WithDefaultValue(definition.DefaultValue as string)
                    .Build();
                return;
            }

            if (definition.ValueType == typeof(int))
            {
                context.AddInputPort<int>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .WithDefaultValue(definition.DefaultValue is int value ? value : 0)
                    .Build();
                return;
            }

            if (definition.ValueType == typeof(float))
            {
                context.AddInputPort<float>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .WithDefaultValue(definition.DefaultValue is float value ? value : 0f)
                    .Build();
                return;
            }

            if (definition.ValueType == typeof(bool))
            {
                context.AddInputPort<bool>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .WithDefaultValue(definition.DefaultValue is bool value && value)
                    .Build();
                return;
            }

            Debug.LogError(
                $"Custom Cozy node '{Title}' has an unsupported input type " +
                $"'{definition.ValueType?.Name}' on '{definition.Name}'.");
        }

        private void AddOutputPort(IPortDefinitionContext context, CozyPortDefinition definition)
        {
            if (definition.ValueType == typeof(string))
            {
                context.AddOutputPort<string>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .Build();
                return;
            }

            if (definition.ValueType == typeof(int))
            {
                context.AddOutputPort<int>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .Build();
                return;
            }

            if (definition.ValueType == typeof(float))
            {
                context.AddOutputPort<float>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .Build();
                return;
            }

            if (definition.ValueType == typeof(bool))
            {
                context.AddOutputPort<bool>(definition.Name)
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .Build();
                return;
            }

            Debug.LogError(
                $"Custom Cozy node '{Title}' has an unsupported output type " +
                $"'{definition.ValueType?.Name}' on '{definition.Name}'.");
        }
    }
}
