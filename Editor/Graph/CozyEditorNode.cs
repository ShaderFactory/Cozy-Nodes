using ShaderFactory.CozyNodes.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Editor
{
    [Serializable]
    public class CozyEditorNode : Node
    {
        public List<CozyRuntimePort> runtimePorts;

        public override void OnEnable()
        {
            base.OnEnable();

            if (TryGetDefaultColor(out Color color))
                DefaultColor = color;
        }

        /// <summary>
        /// Gets the attribute-defined color for this node type. Assigning it to
        /// Graph Toolkit's DefaultColor keeps a graph author's user-selected
        /// ElementColor intact.
        /// </summary>
        protected virtual bool TryGetDefaultColor(out Color color)
        {
            CozyNodeAccentColorAttribute accentColorAttribute =
                GetType().GetCustomAttribute<CozyNodeAccentColorAttribute>(true);

            if (accentColorAttribute != null)
            {
                color = new Color(
                    accentColorAttribute.Red,
                    accentColorAttribute.Green,
                    accentColorAttribute.Blue,
                    accentColorAttribute.Alpha);
                return true;
            }

            CozyNodeColorAttribute colorAttribute =
                GetType().GetCustomAttribute<CozyNodeColorAttribute>(true);

            if (colorAttribute == null)
            {
                color = default;
                return false;
            }

            color = new Color(
                colorAttribute.Red,
                colorAttribute.Green,
                colorAttribute.Blue,
                colorAttribute.Alpha);
            return true;
        }

        public virtual RuntimeCozyNode CreateRuntimeNode(string _nodeID, string _nodeType, RuntimeCozyGraph _graph)
        {
            return new RuntimeCozyNode(_nodeID, _nodeType, _graph);
        }

        /// <summary>
        /// Tells the importer whether an input is an execution-flow port.
        /// Flow ports move execution forward, so they are not imported as values.
        /// Value inputs keep returning false and are evaluated on demand at runtime.
        /// </summary>
        public virtual bool IsFlowInputPort(string portName)
        {
            return false;
        }

        /// <summary>
        /// Tells the importer whether an output is an execution-flow port.
        /// A flow output is saved as a named connection, such as "out", "True",
        /// or "False". Value outputs keep returning false.
        /// </summary>
        public virtual bool IsFlowOutputPort(string portName)
        {
            return false;
        }

        protected override void OnDefinePorts(IPortDefinitionContext c)
        {

        }
    }
}
