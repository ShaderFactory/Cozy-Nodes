using System;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Gives a custom CozyNode access to the graph execution currently running.
    /// It intentionally exposes graph behavior instead of Graph Toolkit editor
    /// objects, so custom nodes work in a built game as well as in the Editor.
    /// </summary>
    public sealed class CozyNodeContext
    {
        private readonly RuntimeCozyNode runtimeNode;

        /// <summary>
        /// The manager running this graph. A game node can use it to invoke Cozy
        /// events, access runtime Blackboard variables, or reach game-owned code.
        /// </summary>
        public CozyManager Manager { get; }

        internal CozyNodeContext(RuntimeCozyNode runtimeNode, CozyManager manager)
        {
            this.runtimeNode = runtimeNode;
            Manager = manager;
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
    }
}
