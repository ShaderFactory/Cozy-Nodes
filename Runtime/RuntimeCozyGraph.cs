using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    public class RuntimeCozyGraph : ScriptableObject
    {
        public string EntryNodeID;

        [SerializeReference] public List<RuntimeCozyNode> AllNodes = new();

    }
}
