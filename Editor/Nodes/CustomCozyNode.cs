using System;
using System.Collections.Generic;
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
        [NonSerialized] private string definedTypeParameterLayout;

        private static readonly MethodInfo TryGetOptionValueMethod = FindTryGetOptionValueMethod();

        public string CustomNodeTypeName => customNodeTypeName;

        /// <summary>
        /// An unresolved type parameter has no concrete runtime representation.
        /// Importers use this to reject a graph before an object-typed port could
        /// slip into a player build.
        /// </summary>
        internal bool HasUnresolvedTypeParameters
        {
            get
            {
                CozyNodeDefinition definition = GetDefinition();
                if (definition == null)
                    return false;

                foreach (CozyTypeParameterDefinition typeParameter in definition.TypeParameters)
                {
                    if (GetSelectedTypeParameterType(typeParameter.Name) == null)
                        return true;
                }

                return false;
            }
        }

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

            foreach (CozyTypeParameterDefinition typeParameter in definition.TypeParameters)
            {
                context.AddOption(
                        GetTypeParameterOptionName(typeParameter.Name),
                        CozyValueTypeRegistry.GetTypeParameterSelectionOptionType(
                            typeParameter.AllowArrayTypes))
                    .WithDisplayName(typeParameter.DisplayName)
                    .WithTooltip(
                        "Choose the concrete type used by every port that shares this parameter.")
                    .WithDefaultValue(
                        CozyValueTypeRegistry.GetDefaultTypeParameterSelection(
                            typeParameter.AllowArrayTypes))
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
            definedTypeParameterLayout = GetTypeParameterSelectionLayout(definition);
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
            if (definition == null)
                return;

            string currentInputListLayout = GetInputListLayout(definition);
            string currentTypeParameterLayout = GetTypeParameterSelectionLayout(definition);
            if (currentInputListLayout == definedInputListLayout &&
                currentTypeParameterLayout == definedTypeParameterLayout)
            {
                return;
            }

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
                port.TypeParameterName = inputList.TypeParameterName;
                port.TypeParameterIsArray = inputList.TypeParameterIsArray;
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

        /// <summary>
        /// Returns the author-selected concrete type for every type parameter.
        /// It is part of the node's structural layout: changing it requires
        /// Graph Toolkit to rebuild the ports with their new types.
        /// </summary>
        private string GetTypeParameterSelectionLayout(CozyNodeDefinition definition)
        {
            if (definition.TypeParameters.Count == 0)
                return string.Empty;

            System.Text.StringBuilder layout = new System.Text.StringBuilder();
            foreach (CozyTypeParameterDefinition typeParameter in definition.TypeParameters)
            {
                Type resolvedType = GetSelectedTypeParameterType(typeParameter.Name);
                layout.Append(typeParameter.Name);
                layout.Append('=');
                layout.Append(resolvedType?.AssemblyQualifiedName ?? typeof(object).AssemblyQualifiedName);
                layout.Append(';');
            }

            return layout.ToString();
        }

        private static string GetTypeParameterOptionName(string typeParameterName)
        {
            return "__cozy_type_" + typeParameterName;
        }

        private Type GetSelectedTypeParameterType(string typeParameterName)
        {
            CozyTypeParameterDefinition typeParameter = GetDefinition()
                ?.FindTypeParameter(typeParameterName);
            if (typeParameter == null)
                return null;

            INodeOption typeOption = GetNodeOptionByName(
                GetTypeParameterOptionName(typeParameterName));
            if (typeOption == null)
                return null;

            if (typeParameter.AllowArrayTypes &&
                typeOption.TryGetValue(
                    out CozyValueTypeParameterSelection valueSelection) &&
                CozyValueTypeRegistry.TryGetTypeParameterValueType(
                    valueSelection,
                    out Type valueType))
            {
                return valueType;
            }

            // Existing graphs saved Set Variable as the scalar-only enum. Keep
            // accepting that representation while Graph Toolkit rewrites the
            // node using the new value-capable picker.
            return typeOption.TryGetValue(out CozyTypeParameterSelection scalarSelection) &&
                   CozyValueTypeRegistry.TryGetTypeParameterValueType(
                       scalarSelection,
                       out Type scalarType)
                ? scalarType
                : null;
        }

        internal bool TryGetTypeParameterPortDefinition(
            string portName,
            out string typeParameterName,
            out bool isArray)
        {
            return TryGetTypeParameterPortDefinition(
                GetDefinition(),
                portName,
                out typeParameterName,
                out isArray);
        }

        /// <summary>
        /// Returns the type Cozy will persist for this port. Generic port models
        /// can still display object while Graph Toolkit is rebuilding the canvas;
        /// imported runtime data must instead use Cozy's resolved T or T[].
        /// </summary>
        internal Type GetResolvedPortValueType(IPort port)
        {
            if (port == null || !TryGetTypeParameterPortDefinition(
                    port.Name,
                    out string typeParameterName,
                    out bool isArray))
            {
                return port?.DataType;
            }

            Type parameterType = GetSelectedTypeParameterType(typeParameterName);
            if (parameterType == null)
                return port.DataType;

            return isArray ? parameterType.MakeArrayType() : parameterType;
        }

        /// <summary>
        /// Tests a prospective wire against the author-selected concrete type.
        /// </summary>
        internal bool CanConnectTypeParameterPort(IPort typeParameterPort, IPort otherPort)
        {
            if (typeParameterPort == null || otherPort == null ||
                typeParameterPort.Direction == otherPort.Direction)
            {
                return false;
            }

            CozyNodeDefinition definition = GetDefinition();
            if (!TryGetTypeParameterPortDefinition(
                    definition,
                    typeParameterPort.Name,
                    out string typeParameterName,
                    out bool isArray))
            {
                return false;
            }

            Type parameterType = GetSelectedTypeParameterType(typeParameterName);
            if (parameterType == null)
                return false;

            Type typeParameterPortType = isArray
                ? parameterType.MakeArrayType()
                : parameterType;

            if (typeParameterPort.Direction == PortDirection.Output)
            {
                return CozyValueConverter.CanConvert(
                    typeParameterPortType,
                    otherPort.DataType);
            }

            return otherPort.DataType == typeParameterPortType;
        }

        internal bool RequiresBlackboardVariablePort(string portName)
        {
            CozyNodeDefinition definition = GetDefinition();
            if (definition == null)
                return false;

            foreach (CozyPortDefinition input in definition.Inputs)
            {
                if (input.Name == portName)
                    return input.RequiresBlackboardVariable;
            }

            return false;
        }

        private static bool TryGetTypeParameterPortDefinition(
            CozyNodeDefinition definition,
            string portName,
            out string typeParameterName,
            out bool isArray)
        {
            typeParameterName = null;
            isArray = false;
            if (definition == null)
                return false;

            foreach (CozyPortDefinition portDefinition in definition.Inputs)
            {
                if (portDefinition.Name != portName)
                    continue;

                typeParameterName = portDefinition.TypeParameterName;
                isArray = portDefinition.TypeParameterIsArray;
                return !string.IsNullOrEmpty(typeParameterName);
            }

            foreach (CozyPortDefinition portDefinition in definition.Outputs)
            {
                if (portDefinition.Name != portName)
                    continue;

                typeParameterName = portDefinition.TypeParameterName;
                isArray = portDefinition.TypeParameterIsArray;
                return !string.IsNullOrEmpty(typeParameterName);
            }

            foreach (CozyInputListDefinition inputList in definition.InputLists)
            {
                if (!CozyNodeReflection.TryGetInputListPortIndex(
                        inputList.Name,
                        portName,
                        out _))
                {
                    continue;
                }

                typeParameterName = inputList.TypeParameterName;
                isArray = inputList.TypeParameterIsArray;
                return !string.IsNullOrEmpty(typeParameterName);
            }

            return false;
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
            if (!string.IsNullOrEmpty(definition.TypeParameterName))
            {
                AddTypeParameterInputPort(
                    context,
                    definition,
                    ResolvePortValueType(definition));
                return;
            }

            // A text area is presentation metadata for a normal string port.
            // All other supported graph values use the same typed builder path.
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

            // Keep this validation at the editor boundary so an unsupported
            // game field never creates a graph port that runtime cannot save.
            if (!CozyValueTypeRegistry.IsSupported(definition.ValueType))
            {
                Debug.LogError(
                    $"Custom Cozy node '{Title}' has an unsupported input type " +
                    $"'{definition.ValueType?.Name}' on '{definition.Name}'.");
                return;
            }

            ITypedInputPortBuilder builder = context.AddInputPort(definition.Name)
                .WithDataType(definition.ValueType)
                .WithDisplayName(definition.DisplayName)
                .WithTooltip(definition.Tooltip);

            // Arrays remain wire values rather than inline list fields. Nodes
            // construct them through Array Make or retrieve them from variables.
            if (!definition.ValueType.IsArray)
                builder.WithDefaultValue(definition.DefaultValue);

            builder.Build();
        }

        private void AddOutputPort(IPortDefinitionContext context, CozyPortDefinition definition)
        {
            if (!string.IsNullOrEmpty(definition.TypeParameterName))
            {
                context.AddOutputPort(definition.Name)
                    .WithDataType(ResolvePortValueType(definition))
                    .WithDisplayName(definition.DisplayName)
                    .WithTooltip(definition.Tooltip)
                    .Build();
                return;
            }

            if (!CozyValueTypeRegistry.IsSupported(definition.ValueType))
            {
                Debug.LogError(
                    $"Custom Cozy node '{Title}' has an unsupported output type " +
                    $"'{definition.ValueType?.Name}' on '{definition.Name}'.");
                return;
            }

            context.AddOutputPort(definition.Name)
                .WithDataType(definition.ValueType)
                .WithDisplayName(definition.DisplayName)
                .WithTooltip(definition.Tooltip)
                .Build();
        }

        private Type ResolvePortValueType(CozyPortDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.TypeParameterName))
                return definition.ValueType;

            Type valueType = GetSelectedTypeParameterType(definition.TypeParameterName);
            if (valueType == null)
                return typeof(object);

            return definition.TypeParameterIsArray
                ? valueType.MakeArrayType()
                : valueType;
        }

        private static void AddTypeParameterInputPort(
            IPortDefinitionContext context,
            CozyPortDefinition definition,
            Type valueType)
        {
            ITypedInputPortBuilder builder = context.AddInputPort(definition.Name)
                .WithDataType(valueType)
                .WithDisplayName(definition.DisplayName)
                .WithTooltip(definition.Tooltip);

            if (valueType != typeof(object) && !valueType.IsArray)
            {
                builder.WithDefaultValue(
                    CozyValueTypeRegistry.CreateDefaultValue(valueType));
            }

            builder.Build();
        }
    }
}
