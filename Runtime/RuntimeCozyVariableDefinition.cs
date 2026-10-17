using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Serializable description of one Blackboard variable. This belongs to the
    /// imported graph asset and is never changed while the game is running.
    /// </summary>
    [Serializable]
    public class RuntimeCozyVariableDefinition
    {
        public string VariableID;
        public string Name;
        public string ValueTypeName;
        /// <summary>
        /// Initial value for this variable. It uses the same scalar-or-array
        /// representation as ordinary runtime ports, without pretending a
        /// Blackboard default is itself a graph connection.
        /// </summary>
        public CozySerializedValue DefaultValue = new CozySerializedValue();
    }
}
