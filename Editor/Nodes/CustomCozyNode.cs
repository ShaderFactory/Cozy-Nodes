using System;
using System.Reflection;
using ShaderFactory.CozyNodes.Runtime;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Editor
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

        [NonSerialized] private string definedInputListLayout;

        private static readonly MethodInfo TryGetOptionValueMethod = FindTryGetOptionValueMethod();

        public string CustomNodeTypeName => customNodeTypeName;

        protected override bool TryGetDefaultColor(out Color color)
        {
            CozyNodeDefinition definition = GetDefinition();
            if (definition != null && definition.HasAccentColor)
            {
                color = definition.AccentColor;
                return true;
            }

            if (definition != null && definition.HasColor)
            {
                color = definition.Color;
                return true;
            }

            return base.TryGetDefaultColor(out color);
        }

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
            CopyOptionsToRuntimeNode(runtimeNode);
            return runtimeNode;
        }

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            CozyNodeDefinition definition = GetDefinition();
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

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            CozyNodeDefinition definition = GetDefinition();
            if (definition == null)
                return;

            foreach (string flowInputName in definition.FlowInputs)
                context.AddInputPort(flowInputName).Build();

            foreach (CozyPortDefinition input in definition.Inputs)
                AddInputPort(context, input);

            foreach (CozyInputListDefinition inputList in definition.InputLists)
                AddInputListPorts(context, definition, inputList);

            // Flow paths come first on action nodes. This keeps the ordinary
            // execution route readable and leaves value results, such as a For
            // node's Index, at the end of the output section.
            foreach (string flowOutputName in definition.FlowOutputs)
                context.AddOutputPort(flowOutputName).Build();

            foreach (CozyPortDefinition output in definition.Outputs)
                AddOutputPort(context, output);

            definedInputListLayout = GetInputListLayout(definition);
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

        /// <summary>
        /// Rebuilds this node's ports when an option changes the number of ports
        /// in an InputList. CozyGraph calls this after Graph Toolkit records an
        /// option edit, so the option's serialized value is always the source of
        /// truth for the node structure.
        /// </summary>
        internal void RefreshInputListPorts()
        {
            CozyNodeDefinition definition = GetDefinition();
            if (definition == null || definition.InputLists.Count == 0)
                return;

            string currentLayout = GetInputListLayout(definition);
            if (currentLayout == definedInputListLayout)
                return;

            WarnAboutRemovedInputListConnections(definition);
            DefineNode();
        }

        private CozyNodeDefinition GetDefinition()
        {
            return CozyNodeReflection.GetDefinition(ResolveCustomNodeType());
        }

        /// <summary>
        /// Gets the public CozyNode type represented by this adapter. Graph setup
        /// uses this to create the same concrete adapter that the node library
        /// uses, preserving Graph Toolkit copy and duplicate behavior.
        /// </summary>
        internal Type ResolveCustomNodeType()
        {
            ConfigureGeneratedNodeType();
            return CozyNodeReflection.FindNodeType(customNodeTypeName);
        }

        /// <summary>
        /// Reads one authored option using the same path used during import. Entry
        /// nodes use this for their event name, which is configuration rather than
        /// a value port.
        /// </summary>
        internal object GetConfiguredOptionValue(string optionName)
        {
            CozyNodeDefinition definition = GetDefinition();
            CozyOptionDefinition optionDefinition = definition?.FindOption(optionName);
            if (optionDefinition == null)
                return null;

            return GetOptionValue(
                GetNodeOptionByName(optionName),
                optionDefinition);
        }

        private void ConfigureGeneratedNodeType()
        {
            Type generatedNodeType = GetCustomNodeType();
            if (generatedNodeType != null)
            {
                // A generated adapter always knows its concrete game-node type.
                // Refresh the saved assembly-qualified name when it changes, for
                // example after the Cozy runtime assembly is renamed.
                if (customNodeTypeName != generatedNodeType.AssemblyQualifiedName)
                    Configure(generatedNodeType);

                return;
            }

            // Older serialized instances can have no generated adapter. Keep
            // their saved type name untouched because it is their only source.
        }

        private void AddInputListPorts(
            IPortDefinitionContext context,
            CozyNodeDefinition nodeDefinition,
            CozyInputListDefinition inputList)
        {
            int inputCount = GetInputListCount(nodeDefinition, inputList);
            for (int index = 0; index < inputCount; index++)
            {
                CozyPortDefinition port = new CozyPortDefinition();
                port.Name = CozyNodeReflection.GetInputListPortName(inputList.Name, index);
                port.DisplayName = inputList.DisplayName + " " + (index + 1);
                port.Tooltip = inputList.Tooltip;
                port.ValueType = inputList.ElementType;
                port.IsInput = true;
                port.DefaultValue = inputList.GetDefaultValue(index);
                AddInputPort(context, port);
            }
        }

        private int GetInputListCount(
            CozyNodeDefinition nodeDefinition,
            CozyInputListDefinition inputList)
        {
            int count = inputList.MinimumCount;
            CozyOptionDefinition countOption = nodeDefinition.FindOption(inputList.CountOptionName);
            if (countOption?.DefaultValue is int defaultCount)
                count = defaultCount;

            INodeOption graphToolkitOption = GetNodeOptionByName(inputList.CountOptionName);
            if (graphToolkitOption != null && graphToolkitOption.TryGetValue(out int configuredCount))
                count = configuredCount;

            return Math.Clamp(count, inputList.MinimumCount, inputList.MaximumCount);
        }

        private string GetInputListLayout(CozyNodeDefinition definition)
        {
            if (definition.InputLists.Count == 0)
                return string.Empty;

            System.Text.StringBuilder layout = new System.Text.StringBuilder();
            foreach (CozyInputListDefinition inputList in definition.InputLists)
            {
                layout.Append(inputList.Name);
                layout.Append('=');
                layout.Append(GetInputListCount(definition, inputList));
                layout.Append(';');
            }

            return layout.ToString();
        }

        private void WarnAboutRemovedInputListConnections(CozyNodeDefinition definition)
        {
            foreach (CozyInputListDefinition inputList in definition.InputLists)
            {
                int retainedCount = GetInputListCount(definition, inputList);
                int removedConnectionCount = 0;

                foreach (IPort port in GetInputPorts())
                {
                    if (CozyNodeReflection.TryGetInputListPortIndex(
                            inputList.Name,
                            port.Name,
                            out int index) &&
                        index >= retainedCount &&
                        port.IsConnected)
                    {
                        removedConnectionCount++;
                    }
                }

                if (removedConnectionCount > 0)
                {
                    Debug.LogWarning(
                        $"Reducing '{inputList.DisplayName}' on '{Title}' removes " +
                        $"{removedConnectionCount} connected input(s).");
                }
            }
        }

        private void CopyOptionsToRuntimeNode(ReflectedCozyRuntimeNode runtimeNode)
        {
            CozyNodeDefinition definition = GetDefinition();
            if (definition == null)
                return;

            foreach (CozyOptionDefinition optionDefinition in definition.Options)
            {
                INodeOption graphToolkitOption = GetNodeOptionByName(optionDefinition.Name);
                object optionValue = GetOptionValue(graphToolkitOption, optionDefinition);
                runtimeNode.RegisterOption(
                    optionDefinition.Name,
                    optionValue,
                    optionDefinition.ValueType);
            }
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

        private static object GetOptionValue(
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
