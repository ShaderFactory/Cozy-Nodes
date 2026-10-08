using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// One execution-flow connection imported from a named editor output port.
    /// </summary>
    [Serializable]
    public class RuntimeFlowConnection
    {
        public string OutputPortName;
        public string NextNodeID;
    }
}
