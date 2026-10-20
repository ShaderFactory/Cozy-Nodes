using System;
using System.Collections.Generic;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Gives a custom CozyNode access to the graph execution currently running.
    /// It intentionally exposes graph behavior instead of Graph Toolkit editor
    /// objects, so custom nodes work in a built game as well as in the Editor.
    /// </summary>
    public class CozyNodeContext
    {
        private readonly RuntimeCozyNode runtimeNode;

        /// <summary>
        /// The manager running this graph. A game node can use it to invoke Cozy
        /// events, access runtime Blackboard variables, or reach game-owned code.
        /// </summary>
        public CozyManager Manager { get; }

        /// <summary>
        /// The named flow input that started this execution. It is null when a
        /// node resumes after a scoped child flow, such as a For Body iteration.
        /// </summary>
        public string EnteredFlowInputName { get; }

        internal CozyNodeContext(
            RuntimeCozyNode runtimeNode,
            CozyManager manager,
            string enteredFlowInputName = null)
        {
            this.runtimeNode = runtimeNode;
            Manager = manager;
            EnteredFlowInputName = enteredFlowInputName;
        }

        /// <summary>
        /// Resolves an input by following its connection recursively. If it is not
        /// connected, this returns the value entered directly in the graph node.
        /// </summary>
        public object GetInputValue(string inputPortName)
        {
            if (runtimeNode == null)
                return null;

            return runtimeNode.GetInputValue(inputPortName, Manager);
        }

        /// <summary>
        /// Resolves an input and converts it to the requested supported Cozy type.
        /// This uses the same conversion rules that validate graph connections.
        /// </summary>
        public T GetInputValue<T>(string inputPortName)
        {
            object value = GetInputValue(inputPortName);
            if (value is T typedValue)
                return typedValue;

            if (CozyValueConverter.TryConvert(value, typeof(T), out object convertedValue) &&
                convertedValue is T convertedTypedValue)
            {
                return convertedTypedValue;
            }

            return default;
        }

        /// <summary>
        /// Resolves every port declared by an InputList in its displayed order.
        /// A list input uses the same connection and conversion behavior as a
        /// regular Input field.
        /// </summary>
        public List<T> GetInputValues<T>(string inputListName)
        {
            List<T> typedValues = new List<T>();
            if (runtimeNode == null)
                return typedValues;

            List<object> values = runtimeNode.GetInputValues(inputListName, Manager);
            foreach (object value in values)
            {
                if (value is T typedValue)
                {
                    typedValues.Add(typedValue);
                    continue;
                }

                if (CozyValueConverter.TryConvert(value, typeof(T), out object convertedValue) &&
                    convertedValue is T convertedTypedValue)
                {
                    typedValues.Add(convertedTypedValue);
                    continue;
                }

                typedValues.Add(default);
            }

            return typedValues;
        }

        /// <summary>
        /// Returns the concrete type currently assigned to one output. Generic
        /// Cozy nodes use this to construct a value whose element type was
        /// selected by the graph author.
        /// </summary>
        public Type GetOutputValueType(string outputPortName)
        {
            return runtimeNode?.GetPortByName(outputPortName)?.GetValueType();
        }

        /// <summary>
        /// Gets the Blackboard reference carried by an input connected directly to
        /// a Blackboard variable node. This deliberately differs from
        /// <see cref="GetInputValue"/>, which resolves that reference to the
        /// variable's current value.
        /// </summary>
        public bool TryGetInputVariableReference(
            string inputPortName,
            out RuntimeVariableReference variableReference)
        {
            variableReference = null;
            CozyRuntimePort inputPort = runtimeNode?.GetPortByName(inputPortName);
            if (inputPort?.type != CozyRuntimePort.PortType.Variable)
                return false;

            variableReference = inputPort.variableReference;
            return variableReference != null &&
                   !string.IsNullOrEmpty(variableReference.variableID);
        }

        /// <summary>
        /// Gets configuration stored in this node's Options section. Options are
        /// local to the node and are never read from graph connections.
        /// </summary>
        public T GetOptionValue<T>(string optionName)
        {
            if (runtimeNode == null ||
                !runtimeNode.TryGetOptionValue(optionName, out object value))
            {
                return default;
            }

            return value is T typedValue ? typedValue : default;
        }
    }
}
