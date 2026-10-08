using System;
using System.Collections.Generic;
using UnityEngine;


namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Represents a node after being converted to runtime form.
    /// This runtime version of the node is what gets saved in the .cozygraph asset file.
    /// It is also the version that evaluates ports and executes in-game runtime logic
    /// </summary>
    [Serializable]
    public class RuntimeCozyNode 
    {
        /// <summary>
        /// Stable identifier copied from the Graph Toolkit editor node during import.
        /// </summary>
        public string NodeID;

        /// <summary> Identifies editor node class name. </summary>
        public string NodeType;

        /// <summary>
        /// The execution paths leaving this node. Each path is named after an editor
        /// flow output, allowing nodes such as Branch to choose a path at runtime.
        /// </summary>
        [SerializeReference] public List<RuntimeFlowConnection> FlowOutputConnections = new();

        /// <summary> Stores all input port values using a class that is serializable </summary>
        [SerializeReference] public List<CozyRuntimePort> InputPorts = new ();

        /// <summary> Stores all input port values using a class that is serializable </summary>
        [SerializeReference] public List<CozyRuntimePort> OutputPorts = new ();

        /// <summary> Reference to what runtime graph this node is a part of. </summary>
        [SerializeReference] public RuntimeCozyGraph Graph;

        #region Constructors
        public RuntimeCozyNode() {}

        /// <summary> Default constructor. Instances of RuntimeCozyNodes are created by the importer (CozyGraphI </summary>
        public RuntimeCozyNode(string _nodeID, string _nodeType, RuntimeCozyGraph _graph)
        {
            NodeID = _nodeID;
            NodeType = _nodeType;
            Graph = _graph;
        }
        #endregion

        /// <summary> Evaluate an output of a specific port. </summary>
        public virtual object GetValue(CozyRuntimePort port, CozyManager cozyManager)
        {
            if (port == null)
            {
                Debug.LogWarning("Tried to evaluate a missing runtime port.");
                return null;
            }

            // A normal port stores its value directly in the runtime asset.
            object value;

            if (port.type == CozyRuntimePort.PortType.Variable)
            {
                if (port.variableReference == null || string.IsNullOrEmpty(port.variableReference.variableID))
                {
                    Debug.LogWarning($"Port '{port.Key}' is missing its Blackboard variable reference.");
                    return null;
                }

                if (cozyManager == null || cozyManager.RuntimeVariables == null)
                {
                    Debug.LogWarning($"Port '{port.Key}' tried to read a Blackboard variable without a running CozyManager.");
                    return null;
                }

                if (!cozyManager.RuntimeVariables.TryGetValue(port.variableReference.variableID, out object variableValue))
                {
                    Debug.LogWarning($"Could not find Blackboard variable '{port.variableReference.variableID}'.");
                    return null;
                }

                value = variableValue;
            }
            else if (port.type != CozyRuntimePort.PortType.Port)
            {
                value = port.GetValue();
            }
            else
            {
                // A connected input stores an address. Follow it backwards and ask the
                // source node to calculate the value of its output port.
                if (port.portReference == null)
                {
                    Debug.LogWarning($"Port '{port.Key}' is marked as connected but has no source.");
                    return null;
                }

                RuntimeCozyNode sourceNode = GetNodeByID(port.portReference.nodeID);
                if (sourceNode == null)
                {
                    Debug.LogWarning($"Could not find node '{port.portReference.nodeID}' for port '{port.Key}'.");
                    return null;
                }

                CozyRuntimePort sourcePort = sourceNode.GetPortByName(port.portReference.portName);
                value = sourceNode.GetValue(sourcePort, cozyManager);
            }

            if (CozyValueConverter.TryConvert(value, port.GetValueType(), out object convertedValue))
                return convertedValue;

            string sourceTypeName = value == null ? "null" : value.GetType().Name;
            string destinationTypeName = port.GetValueType()?.Name ?? "unknown";
            Debug.LogWarning(
                $"Port '{port.Key}' cannot convert {sourceTypeName} to {destinationTypeName}.");
            return null;
        }

        /// <summary> Gets and evaluates one of this node's input ports. </summary>
        public object GetInputValue(string portName, CozyManager cozyManager)
        {
            CozyRuntimePort inputPort = GetPortByName(portName);
            return GetValue(inputPort, cozyManager);
        }

        private RuntimeCozyNode GetNodeByID(string nodeID)
        {
            if (Graph == null)
                return null;

            foreach (RuntimeCozyNode node in Graph.AllNodes)
            {
                if (node.NodeID == nodeID)
                    return node;
            }

            return null;
        }

        /// <summary>
        /// Executes this node's action. Value-only nodes do not run in the flow.
        /// They calculate their outputs through GetValue when another node needs them.
        /// </summary>
        public virtual CozyNodeExecutionResult Run()
        {
            // A node with no action is treated as an end point. This also makes EndNode
            // quiet until it receives its own specialized runtime behavior.
            return CozyNodeExecutionResult.Finish();
        }

        /// <summary>
        /// Executes this node with access to the runner that owns the graph instance.
        /// Existing nodes can keep overriding Run() when they do not need the runner.
        /// Nodes that need to communicate with the game can override this method.
        /// </summary>
        public virtual CozyNodeExecutionResult Run(CozyManager cozyManager)
        {
            return Run();
        }

        /// <summary>
        /// Adds or replaces the node reached from one named flow output.
        /// The importer calls this once for every connected execution output.
        /// </summary>
        public void RegisterFlowOutput(string outputPortName, string nextNodeID)
        {
            foreach (RuntimeFlowConnection connection in FlowOutputConnections)
            {
                if (connection.OutputPortName == outputPortName)
                {
                    connection.NextNodeID = nextNodeID;
                    return;
                }
            }

            RuntimeFlowConnection newConnection = new RuntimeFlowConnection();
            newConnection.OutputPortName = outputPortName;
            newConnection.NextNodeID = nextNodeID;
            FlowOutputConnections.Add(newConnection);
        }

        /// <summary>
        /// Finds the next node for a named flow output chosen by Run.
        /// </summary>
        public bool TryGetFlowOutput(string outputPortName, out string nextNodeID)
        {
            foreach (RuntimeFlowConnection connection in FlowOutputConnections)
            {
                if (connection.OutputPortName == outputPortName)
                {
                    nextNodeID = connection.NextNodeID;
                    return true;
                }
            }

            nextNodeID = null;
            return false;
        }

        /// <summary> Create a CozyRuntimePort entry to the list. </summary>
        public void RegisterPort(string key, object value, bool isOutput, Type valueType)
        {
            // Each port belongs to either the input list or the output list.
            // Keep them separated so a node can use the same name for an input and an output.
            List<CozyRuntimePort> ports;
            if (isOutput)
                ports = OutputPorts;
            else
                ports = InputPorts;

            // If this port was already registered, only update its value.
            foreach (var p in ports)   // If exists, just replace the value..
            {
                if (p.Key == key)
                {
                    p.SetValue(value);
                    p.ValueTypeName = valueType?.AssemblyQualifiedName;
                    return;
                }
            }
            
            CozyRuntimePort entry = new (); // But if not, create a new entry.
            entry.Key = key;
            entry.ValueTypeName = valueType?.AssemblyQualifiedName;
            entry.SetValue(value);
            ports.Add(entry);
        }

        public CozyRuntimePort GetPortByName(string key)
        {
            foreach (var port in InputPorts)
            {
                if (port.Key == key)
                { 
                    return port;
                }
            }

            // Output ports are usually evaluated by another node, but searching them here
            // also makes GetPortByName useful to custom runtime nodes.
            foreach (var port in OutputPorts)
            {
                if (port.Key == key)
                {
                    return port;
                }
            }

            Debug.LogError($"GetPortByName failed to find port named '{key}'");
            return null;
        }
    }

}
