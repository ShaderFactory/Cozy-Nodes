using ShaderFactory.CozyGraphToolkit.Runtime;
using System;
using Unity.GraphToolkit.Editor;


namespace ShaderFactory.CozyGraphToolkit.Editor
{
    [Serializable]
    [Node(
        "Cozy Nodes/Flow",
        null,
        "Print Node",
        "Packages/com.shaderfactory.cozygraphtoolkit/Editor/Styles/PrintNode.uss")]
    public class PrintNode : CozyEditorNode
    {
        public override RuntimeCozyNode CreateRuntimeNode(string nodeID, string nodeType, RuntimeCozyGraph graph)
        {
            // The editor node decides which runtime class should run in the game.
            // PrintNode uses CustomPrintRuntime so its Run method writes the Message value.
            CustomPrintRuntime runtimePrintNode = new CustomPrintRuntime();
            runtimePrintNode.NodeID = nodeID;
            runtimePrintNode.NodeType = nodeType;
            runtimePrintNode.Graph = graph;
            return runtimePrintNode;
        }

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {
            c.AddInputPort("in").Build();
            c.AddOutputPort("out").Build();
            // Graph Toolkit renders this input as a dialogue-sized text area.
            // It is still a normal string port, so the runtime does not change.
            c.AddInputPort<string>("Message")
                .AsTextArea(5, 8)
                .WithTooltip("The text written to the Unity Console.")
                .Build();
        }

        public override bool IsFlowInputPort(string portName)
        {
            return portName == "in";
        }

        public override bool IsFlowOutputPort(string portName)
        {
            return portName == "out";
        }
    }
}
