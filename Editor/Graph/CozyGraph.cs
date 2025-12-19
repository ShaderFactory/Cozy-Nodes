using UnityEngine;
using UnityEditor;
using Unity.GraphToolkit.Editor;
using System;
using System.Collections.Generic;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    [Graph(AssetExtension)]
    public class CozyGraph : Graph
    {
        public const string AssetExtension = "cozygraph";

        [MenuItem("Assets/Create/Shader Factory/Cozy Graph Toolkit/Graph", false)]
        private static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<CozyGraph>();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            Debug.Log("Graph Changed");
            base.OnGraphChanged(graphLogger);

            // Validate all nodes
            foreach (var node in GetNodes())
            {
                CheckMultipleConnectionWarning(node, graphLogger);
            }
        }

        /// <summary>
        /// Check if the node should throw a warning if connecting a single output to multiple nodes is illegal.
        /// </summary>
        private void CheckMultipleConnectionWarning(INode node, GraphLogger graphLogger)
        {
            foreach (var port in node.GetOutputPorts())
            {
                if (node is ICozyRuntimeCreator)
                {
                    var connectedPorts = new List<IPort>();
                    port.GetConnectedPorts(connectedPorts);

                    if (connectedPorts.Count > 1)
                    {
                        graphLogger.LogWarning(
                            $"Output port '{port.displayName}' has {connectedPorts.Count} connections. " +
                            $"Only the first connection will be used at runtime.",
                            node
                        );
                    }
                }
            }
        }
    }
}