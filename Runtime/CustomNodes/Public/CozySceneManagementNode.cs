namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation base for Unity Scene Management nodes. These nodes
    /// intentionally expose scene names and simple values rather than Unity's
    /// Scene struct, which is not a portable Cozy graph value type.
    /// </summary>
    [CozyNodeColor(0.102f, 0.278f, 0.330f)]
    [CozyNodeAccentColor(0.325f, 0.753f, 0.792f)]
    [CozyNodeHeaderTextColor(1f, 1f, 1f)]
    [CozyNodeIcon("Packages/com.shaderfactory.cozynodes/Editor/Icons/SceneManagement.png")]
    public abstract class CozySceneManagementNode : CozyNode
    {
    }
}
