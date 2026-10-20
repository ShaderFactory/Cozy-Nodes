namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Gives a game-authored CozyContext access to its imported ports, options,
    /// and the CozyManager while the context is running.
    /// </summary>
    public sealed class CozyContextContext : CozyNodeContext
    {
        internal CozyContextContext(RuntimeCozyContextNode runtimeContext, CozyManager manager)
            : base(runtimeContext, manager)
        {
        }
    }
}
