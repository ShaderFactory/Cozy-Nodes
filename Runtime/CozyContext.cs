namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Base class for a game-authored ordered context. A context contains
    /// allowed CozyBlock types and can expose its own options and ports.
    /// Runtime execution semantics are added separately from this authoring API.
    /// </summary>
    public abstract class CozyContext : CozyGraphElement
    {
        /// <summary>
        /// Called once when flow enters this context, before its blocks run in
        /// displayed order. Contexts use this for shared setup; individual
        /// actions belong in CozyBlock.Execute.
        /// </summary>
        public virtual void OnEnter(CozyContextContext context)
        {
        }

        /// <summary>
        /// Called after the context completes its blocks or leaves through a
        /// block flow output.
        /// </summary>
        public virtual void OnExit(CozyContextContext context)
        {
        }
    }
}
