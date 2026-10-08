using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Per-execution values copied from a graph's Blackboard definitions.
    /// Each CozyManager receives a separate instance, so playing a graph never
    /// writes values back into its imported .cozygraph asset.
    /// </summary>
    public class CozyRuntimeVariables
    {
        private readonly Dictionary<string, object> valuesByID =
            new Dictionary<string, object>();

        private readonly Dictionary<string, RuntimeCozyVariableDefinition> definitionsByID =
            new Dictionary<string, RuntimeCozyVariableDefinition>();

        public CozyRuntimeVariables(List<RuntimeCozyVariableDefinition> definitions)
        {
            if (definitions == null)
                return;

            foreach (RuntimeCozyVariableDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.VariableID))
                    continue;

                object defaultValue = null;
                if (definition.DefaultValue != null)
                    defaultValue = definition.DefaultValue.GetValue();

                valuesByID[definition.VariableID] = defaultValue;
                definitionsByID[definition.VariableID] = definition;
            }
        }

        /// <summary>
        /// Reads a value by the stable Blackboard variable identifier.
        /// Nodes will use this identifier so renaming a variable does not break them.
        /// </summary>
        public bool TryGetValue(string variableID, out object value)
        {
            return valuesByID.TryGetValue(variableID, out value);
        }

        /// <summary>
        /// Changes a value only for this running graph instance.
        /// The method returns false when the Blackboard did not define the identifier.
        /// </summary>
        public bool TrySetValue(string variableID, object value)
        {
            return TrySetValue(variableID, value, out _);
        }

        /// <summary>
        /// Changes a value only when it matches the data type declared in the
        /// Blackboard. This keeps a generic Set Variable node from silently putting
        /// a string into a boolean or number variable.
        /// </summary>
        public bool TrySetValue(string variableID, object value, out string errorMessage)
        {
            if (!valuesByID.ContainsKey(variableID) || !definitionsByID.TryGetValue(variableID, out RuntimeCozyVariableDefinition definition))
            {
                errorMessage = $"The Blackboard does not contain variable '{variableID}'.";
                return false;
            }

            Type expectedType = null;
            if (!string.IsNullOrEmpty(definition.ValueTypeName))
                expectedType = Type.GetType(definition.ValueTypeName);
            if (value != null && expectedType != null && !expectedType.IsInstanceOfType(value))
            {
                errorMessage = $"Variable '{definition.Name}' expects {expectedType.Name}, but received {value.GetType().Name}.";
                return false;
            }

            valuesByID[variableID] = value;
            errorMessage = null;
            return true;
        }
    }
}
