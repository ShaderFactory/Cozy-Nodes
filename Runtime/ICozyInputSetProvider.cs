using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Supplies the entries of a <see cref="ProvidedInputSetAttribute"/> field.
    /// Each entry becomes one input port on the node.
    ///
    /// Providers are used by the Editor when a node's ports are defined; the
    /// imported runtime graph already contains the resulting ports, so a player
    /// build never needs to call the provider.
    /// </summary>
    public interface ICozyInputSetProvider
    {
        /// <summary>
        /// Returns the current entries in display order. Keys must be stable and
        /// unique: they identify saved values and wires. Entries with an empty
        /// or repeated key are ignored.
        /// </summary>
        IReadOnlyList<CozyProvidedInput> GetInputs();
    }

    /// <summary>One entry of a provided input set.</summary>
    public readonly struct CozyProvidedInput
    {
        /// <summary>Stable identity used in the port name, for example a language code.</summary>
        public readonly string Key;

        /// <summary>Label shown on the port. Defaults to the key.</summary>
        public readonly string DisplayName;

        public readonly string Tooltip;

        public CozyProvidedInput(string key, string displayName = null, string tooltip = null)
        {
            Key = key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? key : displayName;
            Tooltip = tooltip;
        }
    }
}
