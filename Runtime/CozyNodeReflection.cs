using System;
using System.Collections.Generic;
using System.Reflection;

namespace ShaderFactory.CozyGraphToolkit.Runtime
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

            FieldInfo[] fields = nodeType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CozyNode defaultInstance = CreateNodeInstance(nodeType);

            foreach (FieldInfo field in fields)
            {
                InputAttribute inputAttribute = field.GetCustomAttribute<InputAttribute>(true);
                OutputAttribute outputAttribute = field.GetCustomAttribute<OutputAttribute>(true);

                if (inputAttribute == null && outputAttribute == null)
                    continue;

                CozyPortDefinition port = new CozyPortDefinition();
                port.Name = field.Name;
                port.DisplayName = inputAttribute?.DisplayName ?? outputAttribute?.DisplayName;
                if (string.IsNullOrWhiteSpace(port.DisplayName))
                    port.DisplayName = HumanizeName(field.Name);

                port.Tooltip = inputAttribute?.Tooltip ?? outputAttribute?.Tooltip;
                port.ValueType = field.FieldType;
                port.IsInput = inputAttribute != null;
                port.IsOutput = outputAttribute != null;

                CozyTextAreaAttribute textAreaAttribute =
                    field.GetCustomAttribute<CozyTextAreaAttribute>(true);
                if (textAreaAttribute != null)
                {
                    port.IsTextArea = true;
                    port.MinTextAreaLines = textAreaAttribute.MinLines;
                    port.MaxTextAreaLines = textAreaAttribute.MaxLines;
                }

                if (defaultInstance != null && port.IsInput)
                    port.DefaultValue = field.GetValue(defaultInstance);

                if (port.IsInput)
                    definition.Inputs.Add(port);

                if (port.IsOutput)
                    definition.Outputs.Add(port);
            }

            AddFlowPorts(nodeType, definition);
            return definition;
        }

        private static void AddFlowPorts(Type nodeType, CozyNodeDefinition definition)
        {
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

            if (definition.FlowOutputs.Count == 0)
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
        public readonly List<CozyPortDefinition> Inputs = new List<CozyPortDefinition>();
        public readonly List<CozyPortDefinition> Outputs = new List<CozyPortDefinition>();
        public readonly List<string> FlowInputs = new List<string>();
        public readonly List<string> FlowOutputs = new List<string>();
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
        public object DefaultValue;
        public bool IsInput;
        public bool IsOutput;
        public bool IsTextArea;
        public int MinTextAreaLines;
        public int MaxTextAreaLines;
    }
}
