namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Gives a game-authored CozyBlock access to its imported ports, options,
    /// and its CozyGraphSession while its owning context is running.
    /// </summary>
    public sealed class CozyBlockContext : CozyNodeContext
    {
        internal CozyBlockContext(RuntimeCozyBlock runtimeBlock, CozyGraphSession session)
            : base(runtimeBlock, session)
        {
        }
    }
}
