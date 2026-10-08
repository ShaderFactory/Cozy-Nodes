namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Base class for a game-specific Cozy node.
    /// 
    /// Create one class derived from CozyNode, mark its fields with Input and
    /// Output, and Cozy Nodes creates the editor representation internally.
    /// The class stays in the game/runtime assembly and never needs to reference
    /// UnityEditor or Graph Toolkit's editor-only API.
    /// </summary>
    public abstract class CozyNode
    {
        /// <summary>
        /// Calculates a requested value output. Value nodes override this method
        /// and use context.GetInputValue to resolve their connected inputs.
        /// </summary>
        public virtual object GetValue(CozyNodeContext context, string outputPortName)
        {
            return null;
        }

        /// <summary>
        /// Runs a flow node. Nodes that only calculate values do not need to
        /// override this method.
        /// </summary>
        public virtual CozyNodeExecutionResult Execute(CozyNodeContext context)
        {
            return CozyNodeExecutionResult.Finish();
        }
    }
}
