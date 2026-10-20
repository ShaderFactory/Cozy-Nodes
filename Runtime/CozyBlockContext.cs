namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Gives a game-authored CozyBlock access to its imported ports, options,
    /// and the CozyManager while its owning context is running.
    /// </summary>
    public sealed class CozyBlockContext : CozyNodeContext
    {
        internal CozyBlockContext(RuntimeCozyBlock runtimeBlock, CozyManager manager)
            : base(runtimeBlock, manager)
        {
        }
    }
}
