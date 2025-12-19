namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    public interface ICozyRuntimeCreator
    {
        RuntimeCozyNode CreateRuntimeNode(string _nodeID, string _nodeType, RuntimeCozyGraph _nodeGraph)
        {
            return new RuntimeCozyNode(_nodeID, _nodeType, _nodeGraph);
        }
    }
}