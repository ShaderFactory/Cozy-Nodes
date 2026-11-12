namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Shared presentation and authoring option for built-in comparison nodes.
    /// The violet accent matches Cozy's Boolean data type, since every
    /// comparison resolves to a Boolean result.
    /// </summary>
    [CozyNodeAccentColor(0.5960784f, 0.5294118f, 0.8039216f)]
    public abstract class CozyCompareNode<TComparison> : CozyNode
    {
        [Option("Comparison", Tooltip = "The relationship tested between A and B.")]
        public TComparison Comparison;
    }
}
