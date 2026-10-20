using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Runtime
{
    public class RuntimeCozyGraph : ScriptableObject
    {
        /// <summary>
        /// Stable identity of the authored Graph Toolkit graph. Runtime code does
        /// not depend on Graph Toolkit; the Editor uses this only to match a
        /// running graph to its open authoring window for execution visualization.
        /// </summary>
        public string SourceGraphID;

        /// <summary>
        /// All places where this graph can begin. A graph can have one On Start
        /// entry and any number of named On Event entries.
        /// </summary>
        [SerializeReference] public List<RuntimeCozyGraphEntry> Entries = new();

        [SerializeReference] public List<RuntimeCozyNode> AllNodes = new();

        /// <summary>
        /// Blackboard declarations imported from the editor graph. A CozyManager
        /// copies these defaults into CozyRuntimeVariables when execution begins.
        /// </summary>
        [SerializeReference] public List<RuntimeCozyVariableDefinition> VariableDefinitions = new();

        /// <summary>
        /// Finds one imported entry. On Start has no caller-provided name;
        /// On Event entries are identified by their authored event name.
        /// </summary>
        public bool TryGetEntry(
            CozyGraphEntryKind kind,
            string entryName,
            out RuntimeCozyGraphEntry entry)
        {
            foreach (RuntimeCozyGraphEntry candidate in Entries)
            {
                if (candidate == null || candidate.Kind != kind)
                    continue;

                if (kind != CozyGraphEntryKind.CustomEvent ||
                    string.Equals(candidate.Name, entryName, System.StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        /// <summary>
        /// Finds a top-level runtime node or a block owned by an imported
        /// context. Blocks are intentionally not listed in AllNodes because the
        /// graph runner must enter them through their context.
        /// </summary>
        public bool TryGetNode(string nodeID, out RuntimeCozyNode node)
        {
            foreach (RuntimeCozyNode candidate in AllNodes)
            {
                if (candidate == null)
                    continue;

                if (candidate.NodeID == nodeID)
                {
                    node = candidate;
                    return true;
                }

                if (candidate is RuntimeCozyContextNode context &&
                    context.TryGetBlock(nodeID, out RuntimeCozyBlock block))
                {
                    node = block;
                    return true;
                }
            }

            node = null;
            return false;
        }

    }
}
