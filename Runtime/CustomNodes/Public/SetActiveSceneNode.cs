using UnityEngine.SceneManagement;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Selects one already-loaded scene as Unity's active scene. This affects
    /// where newly created GameObjects and lighting settings belong; it does
    /// not decide which scene is rendered.
    /// </summary>
    [CozyNodeMenu(
        "Scene Management",
        "Set Active Scene",
        Tooltip = "Makes an already-loaded scene active for new GameObjects and lighting.")]
    [FlowInput]
    [FlowOutput]
    public sealed class SetActiveSceneNode : CozySceneManagementNode
    {
        [Input(
            "Scene Name",
            Tooltip = "The name of an already-loaded scene to make active.")]
        public string SceneName;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            string sceneName = context.GetInputValue<string>(nameof(SceneName));
            if (string.IsNullOrWhiteSpace(sceneName))
                return CozyNodeExecutionResult.Fail("Set Active Scene needs a Scene Name.");

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return CozyNodeExecutionResult.Fail(
                    "Set Active Scene could not find a loaded scene named '" + sceneName + "'.");
            }

            if (!SceneManager.SetActiveScene(scene))
            {
                return CozyNodeExecutionResult.Fail(
                    "Unity could not make '" + sceneName + "' the active scene.");
            }

            return CozyNodeExecutionResult.ContinueWith("out");
        }
    }
}
