using System.Collections.Generic;
using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    public class CozyManager : MonoBehaviour
    {
        public RuntimeCozyGraph RuntimeGraph;

        private RuntimeCozyNode current;
        private Dictionary<string, RuntimeCozyNode> lookup =
            new Dictionary<string, RuntimeCozyNode>();

        void Start()
        {
            // Check if there is a graph assigned to the inspector
            if (RuntimeGraph == null)
            {
                Debug.LogWarning("Please assign a Graph to the Cozy Manager.");
                return;
            }

            // Build lookup table
            foreach (var n in RuntimeGraph.AllNodes)
                lookup[n.NodeID] = n;

            // Start at entry
            if (!string.IsNullOrEmpty(RuntimeGraph.EntryNodeID))
                GoTo(RuntimeGraph.EntryNodeID);
            else
                Debug.Log("Graph has no entry node.");
        }

        public void GoTo(string id)
        {
            if (!lookup.TryGetValue(id, out current))
            {
                Debug.LogWarning("NodeID not found: " + id);
                return;
            }

            // Debug.LogWarning(current.NodeType);

            current.Run();
        }

        public void Next()
        {
            if (current == null || string.IsNullOrEmpty(current.NextNodeID))
            {
                Debug.Log("Graph ended.");
                return;
            }

            GoTo(current.NextNodeID);
        }
    }
}