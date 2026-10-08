using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using ShaderFactory.CozyGraphToolkit.Runtime;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    /// <summary>
    /// Converts Graph Toolkit data ports into values that can be stored in a
    /// RuntimeCozyNode. This is Editor-only work that happens during import.
    /// </summary>
    internal static class CozyPortValueImporter
    {
        /// <summary>
        /// Returns either a local port value, the value of a Graph Toolkit helper node,
        /// or an address that the runtime can resolve later from another Cozy node.
        /// </summary>
        public static object GetPortValue(IPort port, Dictionary<INode, string> nodeIDs)
        {
            if (port == null)
                return default;

            if (!port.IsConnected)
                return GetDirectPortValue(port);

            IPort connectedPort = port.FirstConnectedPort;
            INode connectedNode = connectedPort.GetNode();

            // Graph Toolkit uses these helper nodes for values placed directly on
            // connections. They become plain values in the imported runtime graph.
            if (connectedNode is IConstantNode constantNode)
                return GetConstantValue(constantNode);

            if (connectedNode is IVariableNode variableNode)
            {
                if (variableNode.Variable == null)
                {
                    Debug.LogWarning("A variable node has no variable assigned.");
                    return null;
                }

                // Blackboard values are deliberately not copied into individual
                // connected ports. The imported reference lets every running
                // CozyManager read its own mutable value table instead.
                return new RuntimeVariableReference(variableNode.Variable.ID.ToString());
            }

            // A regular data connection stays connected at runtime. The value node is
            // evaluated only when another node asks for this input's value.
            return new RuntimePortReference(
                nodeIDs[connectedNode],
                connectedPort.Name);
        }

        /// <summary>
        /// Reads a Graph Toolkit constant by its declared type. Reflection is required
        /// because the Toolkit exposes the value through TryGetValue&lt;T&gt;.
        /// </summary>
        private static object GetConstantValue(IConstantNode constantNode)
        {
            MethodInfo tryGetValueMethod = typeof(IConstantNode)
                .GetMethods()
                .First(method => method.Name == "TryGetValue" && method.IsGenericMethodDefinition);

            MethodInfo typedTryGetValueMethod = tryGetValueMethod.MakeGenericMethod(constantNode.DataType);

            object defaultValue = null;
            if (constantNode.DataType.IsValueType)
                defaultValue = Activator.CreateInstance(constantNode.DataType);

            object[] arguments = { defaultValue };
            bool wasRead = (bool)typedTryGetValueMethod.Invoke(constantNode, arguments);

            if (!wasRead)
            {
                Debug.LogWarning($"Could not read constant value of type '{constantNode.DataType}'.");
                return null;
            }

            return arguments[0];
        }

        /// <summary>
        /// Reads a value written directly into a node input field.
        ///
        /// Graph Toolkit exposes IPort.TryGetValue as a generic method. Reading
        /// it as object can lose the value for reflected ports, because their UI
        /// field is registered with its concrete type such as string or int.
        /// Invoke the method with the port's real data type instead, then return
        /// the boxed value for Cozy's serializable runtime representation.
        /// </summary>
        private static object GetDirectPortValue(IPort port)
        {
            if (port.DataType == null)
                return null;

            MethodInfo tryGetValueMethod = typeof(IPort)
                .GetMethods()
                .First(method => method.Name == "TryGetValue" && method.IsGenericMethodDefinition);

            MethodInfo typedTryGetValueMethod = tryGetValueMethod.MakeGenericMethod(port.DataType);

            object defaultValue = null;
            if (port.DataType.IsValueType)
                defaultValue = Activator.CreateInstance(port.DataType);

            object[] arguments = { defaultValue };
            bool wasRead = (bool)typedTryGetValueMethod.Invoke(port, arguments);

            return wasRead ? arguments[0] : null;
        }

        /// <summary>
        /// Reads the default value configured in the Graph Toolkit Blackboard.
        /// This is used once when importing a variable definition; connections to a
        /// variable use RuntimeVariableReference instead.
        /// </summary>
        internal static object GetVariableDefaultValue(IVariable variable, Type valueType)
        {
            if (variable == null)
            {
                Debug.LogWarning("A variable node has no variable assigned.");
                return null;
            }

            if (valueType == null)
            {
                Debug.LogWarning("Could not determine the value type of a variable connection.");
                return null;
            }

            MethodInfo tryGetDefaultValueMethod = typeof(IVariable)
                .GetMethods()
                .First(method => method.Name == "TryGetDefaultValue" && method.IsGenericMethodDefinition);

            MethodInfo typedTryGetDefaultValueMethod = tryGetDefaultValueMethod.MakeGenericMethod(valueType);

            object defaultValue = null;
            if (valueType.IsValueType)
                defaultValue = Activator.CreateInstance(valueType);

            object[] arguments = { defaultValue };
            bool wasRead = (bool)typedTryGetDefaultValueMethod.Invoke(variable, arguments);

            if (!wasRead)
            {
                Debug.LogWarning($"Could not read default value of variable '{variable}'.");
                return null;
            }

            return arguments[0];
        }
    }
}
