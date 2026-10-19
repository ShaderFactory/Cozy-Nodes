using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.Editor;

namespace ShaderFactory.CozyNodes.Editor
{
    /// <summary>
    /// Translates Graph Toolkit's public subgraph model into the small interface
    /// the Cozy importer needs. GTK expresses every subgraph boundary as a
    /// Blackboard variable, including flow boundaries; it does not expose a
    /// separate public flow-gateway model for subgraphs.
    /// </summary>
    internal static class CozyGraphToolkitSubgraphInterface
    {
        internal sealed class Description
        {
            internal Graph Subgraph { get; }
            internal IReadOnlyList<DataPort> DataInputs { get; }
            internal IReadOnlyList<DataPort> DataOutputs { get; }
            internal IReadOnlyList<FlowPort> FlowInputs { get; }
            internal IReadOnlyList<FlowPort> FlowOutputs { get; }

            internal Description(
                Graph subgraph,
                List<DataPort> dataInputs,
                List<DataPort> dataOutputs,
                List<FlowPort> flowInputs,
                List<FlowPort> flowOutputs)
            {
                Subgraph = subgraph;
                DataInputs = dataInputs;
                DataOutputs = dataOutputs;
                FlowInputs = flowInputs;
                FlowOutputs = flowOutputs;
            }
        }

        internal readonly struct DataPort
        {
            internal IVariable Variable { get; }
            internal IPort ParentPort { get; }

            internal DataPort(IVariable variable, IPort parentPort)
            {
                Variable = variable;
                ParentPort = parentPort;
            }
        }

        internal readonly struct FlowPort
        {
            internal string Name { get; }
            internal IPort ParentPort { get; }
            internal IPort ChildPort { get; }

            internal FlowPort(string name, IPort parentPort, IPort childPort)
            {
                Name = name;
                ParentPort = parentPort;
                ChildPort = childPort;
            }
        }

        internal static bool TryDescribe(
            INode candidateNode,
            out Description description,
            out string errorMessage)
        {
            description = null;
            errorMessage = null;

            if (candidateNode is not ISubgraphNode subgraphNode)
            {
                errorMessage = "The node does not implement Graph Toolkit's ISubgraphNode interface.";
                return false;
            }

            Graph subgraph = subgraphNode.GetSubgraph();
            if (subgraph == null)
            {
                errorMessage = "The subgraph node has no assigned child graph.";
                return false;
            }

            List<DataPort> dataInputs = new List<DataPort>();
            List<DataPort> dataOutputs = new List<DataPort>();
            List<FlowPort> flowInputs = new List<FlowPort>();
            List<FlowPort> flowOutputs = new List<FlowPort>();

            foreach (IVariable variable in subgraph.GetVariables())
            {
                if (variable == null)
                    continue;

                if (TryGetFlowInputChildPort(subgraph, variable, out IPort childOutputPort))
                {
                    if (!TryGetParentPort(candidateNode, variable, true, out IPort parentInputPort))
                    {
                        errorMessage =
                            $"Subgraph flow input '{variable.Name}' has no matching parent port.";
                        return false;
                    }

                    flowInputs.Add(new FlowPort(variable.Name, parentInputPort, childOutputPort));
                    continue;
                }

                if (TryGetFlowOutputChildPort(subgraph, variable, out IPort childInputPort))
                {
                    if (!TryGetParentPort(candidateNode, variable, false, out IPort parentOutputPort))
                    {
                        errorMessage =
                            $"Subgraph flow output '{variable.Name}' has no matching parent port.";
                        return false;
                    }

                    flowOutputs.Add(new FlowPort(variable.Name, parentOutputPort, childInputPort));
                    continue;
                }

                if (variable.DataType == null)
                {
                    errorMessage =
                        $"Subgraph variable '{variable.Name}' has no concrete data type.";
                    return false;
                }

                if (variable.VariableKind == VariableKind.Input)
                {
                    if (!TryGetParentPort(candidateNode, variable, true, out IPort parentInputPort))
                    {
                        errorMessage =
                            $"Subgraph data input '{variable.Name}' has no matching parent port.";
                        return false;
                    }

                    if (parentInputPort.DataType != variable.DataType)
                    {
                        errorMessage =
                            $"Subgraph data input '{variable.Name}' has a different type on its parent port.";
                        return false;
                    }

                    dataInputs.Add(new DataPort(variable, parentInputPort));
                }
                else if (variable.VariableKind == VariableKind.Output)
                {
                    if (!TryGetParentPort(candidateNode, variable, false, out IPort parentOutputPort))
                    {
                        errorMessage =
                            $"Subgraph data output '{variable.Name}' has no matching parent port.";
                        return false;
                    }

                    if (parentOutputPort.DataType != variable.DataType)
                    {
                        errorMessage =
                            $"Subgraph data output '{variable.Name}' has a different type on its parent port.";
                        return false;
                    }

                    dataOutputs.Add(new DataPort(variable, parentOutputPort));
                }
            }

            description = new Description(
                subgraph,
                dataInputs,
                dataOutputs,
                flowInputs,
                flowOutputs);
            return true;
        }

        /// <summary>
        /// Gets the variables whose in-child connections prove that they are Cozy
        /// execution boundaries rather than regular subgraph data values.
        /// </summary>
        internal static HashSet<string> GetFlowGatewayNames(Graph subgraph)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            if (subgraph == null)
                return names;

            foreach (IVariable variable in subgraph.GetVariables())
            {
                if (variable != null &&
                    (TryGetFlowInputChildPort(subgraph, variable, out _) ||
                     TryGetFlowOutputChildPort(subgraph, variable, out _)))
                {
                    names.Add(variable.Name);
                }
            }

            return names;
        }

        /// <summary>
        /// GTK exposes the referenced graph publicly but keeps the local-versus-
        /// asset distinction on its internal subgraph model. Keep that one small
        /// reflective bridge here rather than inferring locality from asset IDs.
        /// Local subgraphs have their own graph identity, so that inference is
        /// incorrect.
        /// </summary>
        internal static bool IsLocalSubgraphNode(INode candidateNode)
        {
            PropertyInfo property = candidateNode?.GetType().GetProperty(
                "IsReferencingLocalSubgraph",
                BindingFlags.Instance | BindingFlags.Public);
            return property?.GetValue(candidateNode) is bool isLocal && isLocal;
        }

        private static bool TryGetFlowInputChildPort(
            Graph subgraph,
            IVariable variable,
            out IPort childOutputPort)
        {
            childOutputPort = null;
            if (variable.VariableKind != VariableKind.Input)
                return false;

            foreach (INode node in subgraph.GetNodes())
            {
                if (node is not IVariableNode ||
                    !string.Equals(node.Title, variable.Name, StringComparison.Ordinal))
                    continue;

                foreach (IPort outputPort in node.GetOutputPorts())
                {
                    IPort connectedPort = outputPort.FirstConnectedPort;
                    if (connectedPort?.GetNode() is CozyEditorNode cozyNode &&
                        cozyNode.IsFlowInputPort(connectedPort.Name))
                    {
                        childOutputPort = outputPort;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetFlowOutputChildPort(
            Graph subgraph,
            IVariable variable,
            out IPort childInputPort)
        {
            childInputPort = null;
            if (variable.VariableKind != VariableKind.Output)
                return false;

            foreach (INode node in subgraph.GetNodes())
            {
                if (node is not IVariableNode ||
                    !string.Equals(node.Title, variable.Name, StringComparison.Ordinal))
                    continue;

                foreach (IPort inputPort in node.GetInputPorts())
                {
                    IPort connectedPort = inputPort.FirstConnectedPort;
                    if (connectedPort?.GetNode() is CozyEditorNode cozyNode &&
                        cozyNode.IsFlowOutputPort(connectedPort.Name))
                    {
                        childInputPort = inputPort;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetParentPort(
            INode subgraphNode,
            IVariable variable,
            bool isInput,
            out IPort parentPort)
        {
            IEnumerable<IPort> ports = isInput
                ? subgraphNode.GetInputPorts()
                : subgraphNode.GetOutputPorts();

            string variableID = variable.ID.ToString();
            foreach (IPort port in ports)
            {
                // GTK keys subgraph ports by the variable's stable ID, while the
                // visible label is the variable name. The ID is the durable link.
                if (string.Equals(port.Name, variableID, StringComparison.Ordinal))
                {
                    parentPort = port;
                    return true;
                }
            }

            // Some GTK versions expose a normalized port identifier through the
            // public wrapper. The display label remains a safe fallback because
            // variable names are unique within one graph.
            foreach (IPort port in ports)
            {
                if (string.Equals(port.DisplayName, variable.Name, StringComparison.Ordinal))
                {
                    parentPort = port;
                    return true;
                }
            }

            parentPort = null;
            return false;
        }

        /// <summary>
        /// Recognizes the child endpoint for an output flow boundary so the
        /// importer can translate it into a runtime return node.
        /// </summary>
        internal static bool TryGetFlowOutputGateway(
            INode node,
            out string gatewayName,
            out IPort childInputPort)
        {
            gatewayName = null;
            childInputPort = null;
            if (node is not IVariableNode)
                return false;

            foreach (IPort inputPort in node.GetInputPorts())
            {
                IPort connectedPort = inputPort.FirstConnectedPort;
                if (connectedPort?.GetNode() is CozyEditorNode cozyNode &&
                    cozyNode.IsFlowOutputPort(connectedPort.Name))
                {
                    gatewayName = node.Title;
                    childInputPort = inputPort;
                    return !string.IsNullOrWhiteSpace(gatewayName);
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the incoming data wire for a child Blackboard output. Unlike a
        /// flow gateway, this endpoint does not execute anything; it simply
        /// exposes the value that feeds it when the subgraph returns.
        /// </summary>
        internal static bool TryGetDataOutputSource(
            Graph subgraph,
            IVariable variable,
            out IPort childInputPort)
        {
            childInputPort = null;
            if (subgraph == null || variable == null)
                return false;

            foreach (INode node in subgraph.GetNodes())
            {
                if (node is not IVariableNode variableNode ||
                    variableNode.Variable == null ||
                    variableNode.Variable.ID.ToString() != variable.ID.ToString())
                {
                    continue;
                }

                foreach (IPort inputPort in node.GetInputPorts())
                {
                    if (!inputPort.IsConnected)
                        continue;

                    childInputPort = inputPort;
                    return true;
                }
            }

            return false;
        }
    }
}
