using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Loads a Build Settings scene asynchronously, pauses this flow while it
    /// loads, and continues once the scene is loaded and active.
    ///
    /// Unlike Load Scene, the flow does not end. This is meant for graphs that
    /// outlive scene changes, such as a persistent Boot or Game Flow graph.
    /// A graph owned by a scene object loaded in Single mode is destroyed with
    /// that scene and never reaches the output.
    ///
    /// The flow is resumed through the session's trigger mechanism with a
    /// private name, so it never collides with game events.
    /// </summary>
    [CozyNodeMenu(
        "Scene Management",
        "Load Scene And Wait",
        Tooltip = "Loads a Build Settings scene and continues this flow once it has finished loading. Use it in graphs that survive scene changes.")]
    [CozyNodeWidth(260)]
    [FlowInput]
    [FlowOutput]
    public sealed class LoadSceneAndWaitNode : CozySceneManagementNode, ICozyNodeTriggerListener
    {
        [Input(
            "Scene Name",
            Tooltip = "A scene name or Build Settings path, without the .unity extension.")]
        public string SceneName;

        [Option(
            "Load Mode",
            Tooltip = "Single replaces loaded scenes. Additive keeps them and adds this scene.")]
        public LoadSceneMode LoadMode = LoadSceneMode.Single;

        // Runtime state of one pending load. A node instance belongs to one
        // graph session, so this is never shared between sessions.
        [NonSerialized] private bool isLoading;
        [NonSerialized] private bool hasLoaded;
        [NonSerialized] private string resumeTriggerName;

        public override CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            // Second run: the load finished and the trigger resumed this flow.
            if (hasLoaded)
            {
                ResetLoadState();
                return CozyNodeExecutionResult.ContinueWith("out");
            }

            if (isLoading)
                return CozyNodeExecutionResult.Wait();

            string sceneName = context.GetInputValue<string>(nameof(SceneName));
            if (string.IsNullOrWhiteSpace(sceneName))
                return CozyNodeExecutionResult.Fail("Load Scene And Wait needs a Scene Name.");

            LoadSceneMode loadMode = context.GetOptionValue<LoadSceneMode>(nameof(LoadMode));

            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, loadMode);
            }
            catch (Exception exception)
            {
                return CozyNodeExecutionResult.Fail(
                    "Load Scene And Wait could not request '" + sceneName + "': " + exception.Message);
            }

            // LoadSceneAsync returns null (and logs) when the scene is not in
            // the Build Settings.
            if (operation == null)
            {
                return CozyNodeExecutionResult.Fail(
                    "Load Scene And Wait could not load '" + sceneName + "'. Is it in the Build Settings?");
            }

            isLoading = true;
            resumeTriggerName = "__CozyNodes.LoadSceneAndWait." + Guid.NewGuid().ToString("N");

            CozyGraphSession session = context.Session;
            string triggerName = resumeTriggerName;
            operation.completed += completedOperation =>
            {
                // Ignore a load that was cancelled (flow stopped) meanwhile.
                if (!isLoading || resumeTriggerName != triggerName)
                    return;

                hasLoaded = true;
                session.Trigger(triggerName);
            };

            return CozyNodeExecutionResult.Wait();
        }

        public bool TryReceiveTrigger(string triggerName, CozyNodeContext context)
        {
            return hasLoaded &&
                   !string.IsNullOrEmpty(resumeTriggerName) &&
                   string.Equals(triggerName, resumeTriggerName, StringComparison.Ordinal);
        }

        public override void CancelFlowScope()
        {
            ResetLoadState();
        }

        private void ResetLoadState()
        {
            isLoading = false;
            hasLoaded = false;
            resumeTriggerName = null;
        }
    }
}
