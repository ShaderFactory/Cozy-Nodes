using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared imported data for a game-authored Cozy node, context, or block.
    /// The runtime asset stores the authored type as a name, while ports and
    /// options remain independent from the game's concrete assembly.
    /// </summary>
    [Serializable]
    public abstract class RuntimeCozyAuthoredElement : RuntimeCozyNode
    {
        public string AuthoredTypeName;
        public string DisplayName;

        public List<RuntimeCozyNodeOption> Options = new List<RuntimeCozyNodeOption>();

        /// <summary>
        /// Adds or replaces one imported local option.
        /// </summary>
        public void RegisterOption(string key, object value, Type valueType)
        {
            foreach (RuntimeCozyNodeOption option in Options)
            {
                if (option.Key != key)
                    continue;

                option.SetValue(value, valueType);
                return;
            }

            RuntimeCozyNodeOption newOption = new RuntimeCozyNodeOption();
            newOption.Key = key;
            newOption.SetValue(value, valueType);
            Options.Add(newOption);
        }

        public override bool TryGetOptionValue(string optionName, out object value)
        {
            foreach (RuntimeCozyNodeOption option in Options)
            {
                if (option.Key != optionName)
                    continue;

                value = option.GetValue();
                return value != null;
            }

            value = null;
            return false;
        }
    }
}
