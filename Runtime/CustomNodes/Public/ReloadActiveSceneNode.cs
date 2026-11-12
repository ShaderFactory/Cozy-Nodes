using System;
using UnityEngine.SceneManagement;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Reloads the active Build Settings scene and ends the graph flow that
    /// requested it.
    /// </summary>
    [CozyNodeMenu(
        "Scene Management",
        "Reload Active Scene",
        Tooltip = "Reloads the active Build Settings scene and ends this graph flow.")]
    [CozyFlowTerminal]
    [FlowInput]
    public sealed class ReloadActiveSceneNode : CozySceneManagementNode
    {
        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.buildIndex < 0)
            {
                return CozyNodeExecutionResult.Fail(
                    "Reload Active Scene needs an active scene included in Build Settings.");
            }

            try
            {
                SceneManager.LoadScene(activeScene.buildIndex, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                return CozyNodeExecutionResult.Fail(
                    "Reload Active Scene could not request '" + activeScene.name + "': " +
                    exception.Message);
            }

            return CozyNodeExecutionResult.Finish();
        }
    }
}
