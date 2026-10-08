using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
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
        public CozyRuntimePort DefaultValue = new CozyRuntimePort();
    }
}
