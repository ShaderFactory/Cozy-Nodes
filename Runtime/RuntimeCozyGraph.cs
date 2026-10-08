using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    public class RuntimeCozyGraph : ScriptableObject
    {
        public string EntryNodeID;

        [SerializeReference] public List<RuntimeCozyNode> AllNodes = new();

        /// <summary>
        /// Blackboard declarations imported from the editor graph. A CozyManager
        /// copies these defaults into CozyRuntimeVariables when execution begins.
        /// </summary>
        [SerializeReference] public List<RuntimeCozyVariableDefinition> VariableDefinitions = new();

    }
}
