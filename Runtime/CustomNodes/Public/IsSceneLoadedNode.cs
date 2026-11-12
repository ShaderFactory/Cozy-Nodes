using UnityEngine.SceneManagement;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Reports whether Unity currently has a loaded scene with the requested
    /// name. This is primarily useful with additive scene setups.
    /// </summary>
    [CozyNodeMenu("Scene Management", "Is Scene Loaded", Tooltip = "True when a scene with this name is currently loaded. Useful with additive scenes.")]
    public sealed class IsSceneLoadedNode : CozySceneManagementNode
    {
        [Input("Scene Name", Tooltip = "The name of the scene to check.")]
        public string SceneName;

        [Output("Is Loaded")]
        public bool IsLoaded;

        public override object GetValue(CozyNodeContext context, string outputPortName)
        {
            if (outputPortName != nameof(IsLoaded))
                return null;

            string sceneName = context.GetInputValue<string>(nameof(SceneName));
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
