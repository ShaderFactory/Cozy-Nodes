using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// One execution-flow connection imported from a named editor output port.
    /// </summary>
    [Serializable]
    internal class RuntimeFlowConnection
    {
        public string OutputPortName;
        public string NextNodeID;
        public string NextInputPortName;
    }
}
