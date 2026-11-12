using System;
using UnityEngine.SceneManagement;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Requests that Unity load a scene from the Build Settings, then finishes
    /// the current graph flow. A scene transition replaces the environment in
    /// which that flow was running, so a later asynchronous node will provide
    /// the explicit "scene loaded" continuation when it is needed.
    /// </summary>
    [CozyNodeMenu(
        "Scene Management",
        "Load Scene",
        Tooltip = "Loads a Build Settings scene by name or path and ends this graph flow.")]
    [CozyFlowTerminal]
    [FlowInput]
    public sealed class LoadSceneNode : CozySceneManagementNode
    {
        [Input(
            "Scene Name",
            Tooltip = "A scene name or Build Settings path, without the .unity extension.")]
        public string SceneName;

        [Option(
            "Load Mode",
            Tooltip = "Single replaces loaded scenes. Additive keeps them and adds this scene.")]
        public LoadSceneMode LoadMode = LoadSceneMode.Single;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            string sceneName = context.GetInputValue<string>(nameof(SceneName));
            if (string.IsNullOrWhiteSpace(sceneName))
                return CozyNodeExecutionResult.Fail("Load Scene needs a Scene Name.");

            LoadSceneMode loadMode = context.GetOptionValue<LoadSceneMode>(nameof(LoadMode));

            try
            {
                SceneManager.LoadScene(sceneName, loadMode);
            }
            catch (Exception exception)
            {
                return CozyNodeExecutionResult.Fail(
                    "Load Scene could not request '" + sceneName + "': " + exception.Message);
            }

            return CozyNodeExecutionResult.Finish();
        }
    }
}
