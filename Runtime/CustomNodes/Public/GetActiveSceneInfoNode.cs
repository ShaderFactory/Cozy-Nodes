using UnityEngine.SceneManagement;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Exposes the useful graph-safe fields of Unity's active Scene struct.
    /// </summary>
    [CozyNodeMenu("Scene Management", "Get Active Scene Info", Tooltip = "Name, path and build index of the active scene.")]
    public sealed class GetActiveSceneInfoNode : CozySceneManagementNode
    {
        [Output("Name")]
        public string Name;

        [Output("Path")]
        public string Path;

        [Output("Build Index")]
        public int BuildIndex;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            switch (outputPortName)
            {
                case nameof(Name):
                    return activeScene.name;
                case nameof(Path):
                    return activeScene.path;
                case nameof(BuildIndex):
                    return activeScene.buildIndex;
                default:
                    return null;
            }
        }
    }
}
