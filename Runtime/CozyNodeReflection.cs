using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Reflection metadata shared by the Editor adapter and the runtime wrapper.
    /// It is cached by node type, so fields and attributes are inspected once
    /// instead of every time a graph evaluates a port.
    /// </summary>
    public static class CozyNodeReflection
    {
        private static readonly Dictionary<Type, CozyNodeDefinition> definitionsByType =
            new Dictionary<Type, CozyNodeDefinition>();

        /// <summary>
        /// Gets the reflection definition for a concrete CozyNode type.
        /// </summary>
        public static CozyNodeDefinition GetDefinition(Type nodeType)
        {
            if (nodeType == null || !typeof(CozyNode).IsAssignableFrom(nodeType))
                return null;

            if (definitionsByType.TryGetValue(nodeType, out CozyNodeDefinition definition))
                return definition;

            definition = CreateDefinition(nodeType);
            definitionsByType.Add(nodeType, definition);
            return definition;
        }

        /// <summary>
        /// Resolves an assembly-qualified type name stored in an imported graph.
        /// </summary>
        public static Type FindNodeType(string assemblyQualifiedTypeName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedTypeName))
                return null;

            Type nodeType = Type.GetType(assemblyQualifiedTypeName);
            if (nodeType == null || !typeof(CozyNode).IsAssignableFrom(nodeType))
                return null;

            return nodeType;
        }

        /// <summary>
        /// Creates a custom node instance. The constructor must be public and take
        /// no parameters so the graph can create one instance per CozyManager.
        /// </summary>
        public static CozyNode CreateNodeInstance(Type nodeType)
        {
            if (nodeType == null || nodeType.IsAbstract ||
                !typeof(CozyNode).IsAssignableFrom(nodeType))
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(nodeType) as CozyNode;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError(
                    $"Could not create Cozy node '{nodeType.FullName}'. " +
                    "Custom Cozy nodes need a public parameterless constructor.\n" +
                    exception.Message);
                return null;
            }
        }

        private static CozyNodeDefinition CreateDefinition(Type nodeType)
        {
            CozyNodeDefinition definition = new CozyNodeDefinition();
            definition.NodeType = nodeType;
            definition.TypeName = nodeType.AssemblyQualifiedName;

            CozyNodeMenuAttribute menuAttribute =
                nodeType.GetCustomAttribute<CozyNodeMenuAttribute>(true);

            definition.Category = menuAttribute?.Category;
            definition.Title = string.IsNullOrWhiteSpace(menuAttribute?.Title)
                ? HumanizeName(nodeType.Name)
                : menuAttribute.Title;
            definition.Tooltip = menuAttribute?.Tooltip;

            CozyNodeWidthAttribute nodeWidthAttribute =
                nodeType.GetCustomAttribute<CozyNodeWidthAttribute>(true);
            if (nodeWidthAttribute != null)
                definition.MinimumWidth = Math.Max(0, nodeWidthAttribute.MinimumWidth);

            CozyNodeColorAttribute nodeColorAttribute =
                nodeType.GetCustomAttribute<CozyNodeColorAttribute>(true);
            if (nodeColorAttribute != null)
            {
                definition.HasColor = true;
                definition.Color = new Color(
                    nodeColorAttribute.Red,
                    nodeColorAttribute.Green,
                    nodeColorAttribute.Blue,
                    nodeColorAttribute.Alpha);
            }

            CozyNodeAccentColorAttribute accentColorAttribute =
                nodeType.GetCustomAttribute<CozyNodeAccentColorAttribute>(true);
            if (accentColorAttribute != null)
            {
                definition.HasAccentColor = true;
                definition.AccentColor = new Color(
                    accentColorAttribute.Red,
                    accentColorAttribute.Green,
                    accentColorAttribute.Blue,
                    accentColorAttribute.Alpha);
            }

            CozyNodeIconAttribute iconAttribute =
                nodeType.GetCustomAttribute<CozyNodeIconAttribute>(true);
            if (iconAttribute != null)
                definition.IconAssetPath = iconAttribute.AssetPath;

            CozyNodeHeaderTextColorAttribute headerTextColorAttribute =
                nodeType.GetCustomAttribute<CozyNodeHeaderTextColorAttribute>(true);
            if (headerTextColorAttribute != null)
            {
                definition.HasHeaderTextColor = true;
                definition.HeaderTextColor = new Color(
                    headerTextColorAttribute.Red,
                    headerTextColorAttribute.Green,
                    headerTextColorAttribute.Blue,
                    headerTextColorAttribute.Alpha);
            }

            CozyGraphEntryAttribute entryAttribute =
                nodeType.GetCustomAttribute<CozyGraphEntryAttribute>(true);
            if (entryAttribute != null)
            {
                definition.Entry = new CozyGraphEntryDefinition();
                definition.Entry.Kind = entryAttribute.Kind;
                definition.Entry.Name = entryAttribute.Name;
                definition.Entry.NameOptionName = entryAttribute.NameOptionName;
                definition.Entry.FlowOutputName = entryAttribute.FlowOutputName;
            }

            foreach (CozyTypeParameterAttribute typeParameterAttribute in
                     nodeType.GetCustomAttributes<CozyTypeParameterAttribute>(true))
            {
                if (string.IsNullOrWhiteSpace(typeParameterAttribute.Name))
                {
                    UnityEngine.Debug.LogError(
                        $"Custom Cozy node '{definition.Title}' declares a type parameter with no name.");
                    continue;
                }

                if (definition.FindTypeParameter(typeParameterAttribute.Name) != null)
                {
                    UnityEngine.Debug.LogError(
                        $"Custom Cozy node '{definition.Title}' declares type parameter " +
                        $"'{typeParameterAttribute.Name}' more than once.");
                    continue;
                }

                definition.TypeParameters.Add(new CozyTypeParameterDefinition
                {
                    Name = typeParameterAttribute.Name,
                    DisplayName = string.IsNullOrWhiteSpace(typeParameterAttribute.DisplayName)
                        ? typeParameterAttribute.Name
                        : typeParameterAttribute.DisplayName,
                    AllowArrayTypes = typeParameterAttribute.AllowArrayTypes
                });
            }

            FieldInfo[] fields = nodeType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CozyNode defaultInstance = CreateNodeInstance(nodeType);

            foreach (FieldInfo field in fields)
            {
                InputAttribute inputAttribute = field.GetCustomAttribute<InputAttribute>(true);
                OutputAttribute outputAttribute = field.GetCustomAttribute<OutputAttribute>(true);
                OptionAttribute optionAttribute = field.GetCustomAttribute<OptionAttribute>(true);
                InputListAttribute inputListAttribute = field.GetCustomAttribute<InputListAttribute>(true);
                CozyTypeParameterPortAttribute typeParameterPortAttribute =
                    field.GetCustomAttribute<CozyTypeParameterPortAttribute>(true);
                CozyBlackboardVariableAttribute blackboardVariableAttribute =
                    field.GetCustomAttribute<CozyBlackboardVariableAttribute>(true);

                if (inputAttribute == null && outputAttribute == null &&
                    optionAttribute == null && inputListAttribute == null)
                    continue;

                if (optionAttribute != null)
                {
                    AddOptionDefinition(definition, field, optionAttribute, defaultInstance);
                    continue;
                }

                if (inputListAttribute != null)
                {
                    AddInputListDefinition(
                        definition,
                        field,
                        inputListAttribute,
                        typeParameterPortAttribute,
                        defaultInstance);
                    continue;
                }

                CozyPortDefinition port = new CozyPortDefinition();
                port.Name = field.Name;
                port.DisplayName = inputAttribute?.DisplayName ?? outputAttribute?.DisplayName;
                if (string.IsNullOrWhiteSpace(port.DisplayName))
                    port.DisplayName = HumanizeName(field.Name);

                port.Tooltip = inputAttribute?.Tooltip ?? outputAttribute?.Tooltip;
                port.ValueType = field.FieldType;
                port.RequiresBlackboardVariable = blackboardVariableAttribute != null;
                if (typeParameterPortAttribute != null)
                {
                    if (field.FieldType != typeof(object))
                    {
                        UnityEngine.Debug.LogError(
                            $"Type-parameter port '{field.Name}' on '{definition.Title}' " +
                            "must use object as its backing field type.");
                    }
                    else if (definition.FindTypeParameter(typeParameterPortAttribute.TypeParameterName) == null)
                    {
                        UnityEngine.Debug.LogError(
                            $"Type-parameter port '{field.Name}' on '{definition.Title}' " +
                            $"references unknown type parameter '{typeParameterPortAttribute.TypeParameterName}'.");
                    }
                    else
                    {
                        port.TypeParameterName = typeParameterPortAttribute.TypeParameterName;
                        port.TypeParameterIsArray = typeParameterPortAttribute.IsArray;
                    }
                }
                port.IsInput = inputAttribute != null;
                port.IsOutput = outputAttribute != null;

                CozyTextAreaAttribute textAreaAttribute =
                    field.GetCustomAttribute<CozyTextAreaAttribute>(true);
                if (textAreaAttribute != null)
                {
                    port.IsTextArea = true;
                    port.MinTextAreaLines = textAreaAttribute.MinLines;
                    port.MaxTextAreaLines = textAreaAttribute.MaxLines;
                    port.MinTextAreaWidth = Math.Max(0, textAreaAttribute.MinimumWidth);
                }

                if (defaultInstance != null && port.IsInput)
                    port.DefaultValue = field.GetValue(defaultInstance);

                if (port.IsInput)
                    definition.Inputs.Add(port);

                if (port.IsOutput)
                    definition.Outputs.Add(port);
            }

            ValidateTypeParameterDefinitions(definition);
            ValidateInputListDefinitions(definition);
            AddFlowPorts(nodeType, definition);
            return definition;
        }

        private static void AddOptionDefinition(
            CozyNodeDefinition definition,
            FieldInfo field,
            OptionAttribute optionAttribute,
            CozyNode defaultInstance)
        {
            if (!IsSupportedOptionType(field.FieldType))
            {
                UnityEngine.Debug.LogError(
                    $"Custom Cozy node '{definition.Title}' has an unsupported option type " +
                    $"'{field.FieldType.Name}' on '{field.Name}'.");
                return;
            }

            CozyOptionDefinition option = new CozyOptionDefinition();
            option.Name = field.Name;
            option.DisplayName = string.IsNullOrWhiteSpace(optionAttribute.DisplayName)
                ? HumanizeName(field.Name)
                : optionAttribute.DisplayName;
            option.Tooltip = optionAttribute.Tooltip;
            option.ValueType = field.FieldType;

            if (defaultInstance != null)
                option.DefaultValue = field.GetValue(defaultInstance);

            definition.Options.Add(option);
        }

        private static void AddInputListDefinition(
            CozyNodeDefinition definition,
            FieldInfo field,
            InputListAttribute inputListAttribute,
            CozyTypeParameterPortAttribute typeParameterPortAttribute,
            CozyNode defaultInstance)
        {
            if (!field.FieldType.IsArray)
            {
                UnityEngine.Debug.LogError(
                    $"Custom Cozy node '{definition.Title}' must declare input list " +
                    $"'{field.Name}' as an array.");
                return;
            }

            Type elementType = field.FieldType.GetElementType();
            bool usesTypeParameter = typeParameterPortAttribute != null;
            if (usesTypeParameter && field.FieldType != typeof(object[]))
            {
                UnityEngine.Debug.LogError(
                    $"Type-parameter input list '{field.Name}' on '{definition.Title}' " +
                    "must use object[] as its backing field type.");
                return;
            }

            if (usesTypeParameter &&
                definition.FindTypeParameter(typeParameterPortAttribute.TypeParameterName) == null)
            {
                UnityEngine.Debug.LogError(
                    $"Type-parameter input list '{field.Name}' on '{definition.Title}' " +
                    $"references unknown type parameter '{typeParameterPortAttribute.TypeParameterName}'.");
                return;
            }

            if (!usesTypeParameter && !IsSupportedPortValueType(elementType))
            {
                UnityEngine.Debug.LogError(
                    $"Custom Cozy node '{definition.Title}' has an unsupported input list " +
                    $"element type '{elementType?.Name}' on '{field.Name}'.");
                return;
            }

            CozyInputListDefinition inputList = new CozyInputListDefinition();
            inputList.Name = field.Name;
            inputList.DisplayName = string.IsNullOrWhiteSpace(inputListAttribute.DisplayName)
                ? HumanizeName(field.Name)
                : inputListAttribute.DisplayName;
            inputList.Tooltip = inputListAttribute.Tooltip;
            inputList.ElementType = elementType;
            if (usesTypeParameter)
            {
                inputList.TypeParameterName = typeParameterPortAttribute.TypeParameterName;
                inputList.TypeParameterIsArray = typeParameterPortAttribute.IsArray;
            }
            inputList.CountOptionName = inputListAttribute.CountOptionName;
            inputList.MinimumCount = Math.Max(0, inputListAttribute.MinimumCount);
            inputList.MaximumCount = Math.Max(
                inputList.MinimumCount,
                inputListAttribute.MaximumCount);

            if (defaultInstance != null && field.GetValue(defaultInstance) is Array defaultValues)
            {
                foreach (object defaultValue in defaultValues)
                    inputList.DefaultValues.Add(defaultValue);
            }

            definition.InputLists.Add(inputList);
        }

        private static void ValidateInputListDefinitions(CozyNodeDefinition definition)
        {
            foreach (CozyInputListDefinition inputList in definition.InputLists)
            {
                CozyOptionDefinition countOption = definition.FindOption(inputList.CountOptionName);
                if (countOption != null && countOption.ValueType == typeof(int))
                    continue;

                UnityEngine.Debug.LogError(
                    $"Custom Cozy node '{definition.Title}' needs an int [Option] named " +
                    $"'{inputList.CountOptionName}' to control input list '{inputList.Name}'.");
            }
        }

        private static void ValidateTypeParameterDefinitions(CozyNodeDefinition definition)
        {
            foreach (CozyTypeParameterDefinition typeParameter in definition.TypeParameters)
            {
                if (!typeParameter.AllowArrayTypes)
                    continue;

                bool createsNestedArray = definition.Inputs.Exists(port =>
                    port.TypeParameterName == typeParameter.Name &&
                    port.TypeParameterIsArray) ||
                    definition.Outputs.Exists(port =>
                        port.TypeParameterName == typeParameter.Name &&
                        port.TypeParameterIsArray) ||
                    definition.InputLists.Exists(inputList =>
                        inputList.TypeParameterName == typeParameter.Name &&
                        inputList.TypeParameterIsArray);

                if (!createsNestedArray)
                    continue;

                UnityEngine.Debug.LogError(
                    $"Custom Cozy node '{definition.Title}' declares '{typeParameter.Name}' " +
                    "with AllowArrayTypes and also uses T[]. Cozy Nodes does not " +
                    "support nested arrays, so its type picker will use scalar choices.");
                typeParameter.AllowArrayTypes = false;
            }
        }

        /// <summary>
        /// Returns whether a field can be rendered and serialized as a node option.
        /// </summary>
        public static bool IsSupportedOptionType(Type valueType)
        {
            return valueType == typeof(string) || valueType == typeof(int) ||
                   valueType == typeof(float) || valueType == typeof(bool) ||
                   (valueType != null && valueType.IsEnum);
        }

        /// <summary>
        /// Returns whether a type can be used by a regular or variable input port.
        /// </summary>
        public static bool IsSupportedPortValueType(Type valueType)
        {
            return CozyValueTypeRegistry.IsSupported(valueType);
        }

        /// <summary>
        /// Generates a stable internal name for one port in an input list. The
        /// display label remains friendly while the generated name preserves
        /// existing values and wires when the list count changes.
        /// </summary>
        public static string GetInputListPortName(string inputListName, int index)
        {
            return inputListName + "__" + index;
        }

        /// <summary>
        /// Tests whether a port name belongs to an input list and returns its
        /// zero-based element index when it does.
        /// </summary>
        public static bool TryGetInputListPortIndex(
            string inputListName,
            string portName,
            out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(inputListName) ||
                string.IsNullOrWhiteSpace(portName))
            {
                return false;
            }

            string prefix = inputListName + "__";
            if (!portName.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(portName.Substring(prefix.Length), out index) && index >= 0;
        }

        private static void AddFlowPorts(Type nodeType, CozyNodeDefinition definition)
        {
            bool isTerminal = nodeType.GetCustomAttribute<CozyFlowTerminalAttribute>(true) != null;
            object[] flowInputs =
                nodeType.GetCustomAttributes(typeof(FlowInputAttribute), true);
            object[] flowOutputs =
                nodeType.GetCustomAttributes(typeof(FlowOutputAttribute), true);

            foreach (FlowInputAttribute flowInput in flowInputs)
            {
                if (!string.IsNullOrWhiteSpace(flowInput.PortName))
                    definition.FlowInputs.Add(flowInput.PortName);
            }

            foreach (FlowOutputAttribute flowOutput in flowOutputs)
            {
                if (!string.IsNullOrWhiteSpace(flowOutput.PortName))
                    definition.FlowOutputs.Add(flowOutput.PortName);
            }

            // A node that overrides Execute is normally a simple action node. Give
            // it the familiar in/out pair without making every author repeat the
            // same two attributes. Branches can declare their named outputs above.
            if (!OverridesExecute(nodeType))
                return;

            if (definition.FlowInputs.Count == 0)
                definition.FlowInputs.Add("in");

            if (definition.FlowOutputs.Count == 0 && !isTerminal)
                definition.FlowOutputs.Add("out");
        }

        private static bool OverridesExecute(Type nodeType)
        {
            MethodInfo executeMethod = nodeType.GetMethod(
                nameof(CozyNode.Execute),
                BindingFlags.Public | BindingFlags.Instance);

            return executeMethod != null &&
                   executeMethod.DeclaringType != typeof(CozyNode);
        }

        /// <summary>
        /// Changes PascalCase source names into labels that are comfortable to read
        /// in a graph, without requiring authors to write display names everywhere.
        /// </summary>
        public static string HumanizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder(name.Length + 8);
            for (int index = 0; index < name.Length; index++)
            {
                char currentCharacter = name[index];
                bool startsNewWord = index > 0 && char.IsUpper(currentCharacter) &&
                                     !char.IsUpper(name[index - 1]);

                if (startsNewWord)
                    builder.Append(' ');

                builder.Append(currentCharacter);
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// Cached description of a custom Cozy node class.
    /// </summary>
    public sealed class CozyNodeDefinition
    {
        public Type NodeType;
        public string TypeName;
        public string Category;
        public string Title;
        public string Tooltip;
        public int MinimumWidth;
        public bool HasColor;
        public Color Color;
        public bool HasAccentColor;
        public Color AccentColor;
        public string IconAssetPath;
        public bool HasHeaderTextColor;
        public Color HeaderTextColor;
        public CozyGraphEntryDefinition Entry;
        public readonly List<CozyTypeParameterDefinition> TypeParameters = new List<CozyTypeParameterDefinition>();
        public readonly List<CozyPortDefinition> Inputs = new List<CozyPortDefinition>();
        public readonly List<CozyPortDefinition> Outputs = new List<CozyPortDefinition>();
        public readonly List<CozyOptionDefinition> Options = new List<CozyOptionDefinition>();
        public readonly List<CozyInputListDefinition> InputLists = new List<CozyInputListDefinition>();
        public readonly List<string> FlowInputs = new List<string>();
        public readonly List<string> FlowOutputs = new List<string>();

        public CozyOptionDefinition FindOption(string optionName)
        {
            foreach (CozyOptionDefinition option in Options)
            {
                if (option.Name == optionName)
                    return option;
            }

            return null;
        }

        public CozyTypeParameterDefinition FindTypeParameter(string typeParameterName)
        {
            foreach (CozyTypeParameterDefinition typeParameter in TypeParameters)
            {
                if (typeParameter.Name == typeParameterName)
                    return typeParameter;
            }

            return null;
        }
    }

    /// <summary>
    /// Cached entry metadata declared by <see cref="CozyGraphEntryAttribute"/>.
    /// </summary>
    public sealed class CozyGraphEntryDefinition
    {
        public CozyGraphEntryKind Kind;
        public string Name;
        public string NameOptionName;
        public string FlowOutputName;
    }

    /// <summary>
    /// Describes one reflected value port.
    /// </summary>
    public sealed class CozyPortDefinition
    {
        public string Name;
        public string DisplayName;
        public string Tooltip;
        public Type ValueType;
        public string TypeParameterName;
        public bool TypeParameterIsArray;
        public bool RequiresBlackboardVariable;
        public object DefaultValue;
        public bool IsInput;
        public bool IsOutput;
        public bool IsTextArea;
        public int MinTextAreaLines;
        public int MaxTextAreaLines;
        public int MinTextAreaWidth;
    }

    /// <summary>
    /// Describes one node option declared by a game-authored CozyNode.
    /// </summary>
    public sealed class CozyOptionDefinition
    {
        public string Name;
        public string DisplayName;
        public string Tooltip;
        public Type ValueType;
        public object DefaultValue;
    }

    /// <summary>
    /// Describes a repeated group of typed input ports controlled by an int option.
    /// </summary>
    public sealed class CozyInputListDefinition
    {
        public string Name;
        public string DisplayName;
        public string Tooltip;
        public string CountOptionName;
        public Type ElementType;
        public string TypeParameterName;
        public bool TypeParameterIsArray;
        public int MinimumCount;
        public int MaximumCount;
        public readonly List<object> DefaultValues = new List<object>();

        public object GetDefaultValue(int index)
        {
            if (index >= 0 && index < DefaultValues.Count)
                return DefaultValues[index];

            if (ElementType != null && ElementType.IsValueType)
                return Activator.CreateInstance(ElementType);

            return null;
        }
    }

    /// <summary>
    /// A named type variable shared by several ports on one Cozy node.
    /// </summary>
    public sealed class CozyTypeParameterDefinition
    {
        public string Name;
        public string DisplayName;
        public bool AllowArrayTypes;
    }
}
