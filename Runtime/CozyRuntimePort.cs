using System;

namespace ShaderFactory.CozyNodes.Runtime
{
    /// <summary>
    /// Serializable port key/value pair with typed fields
    /// </summary>
    [Serializable]
    public class CozyRuntimePort
    {
        public string ImportMessage;

        public string Key;

        /// <summary>
        /// Assembly-qualified type expected by this port. The importer records it so
        /// runtime evaluation can apply the same permitted conversions as the Editor.
        /// </summary>
        public string ValueTypeName;

        private RuntimeCozyNode node;

        /// <summary>
        /// Local value stored by this port. The wrapper supports both scalar and
        /// one-dimensional array values without adding a bespoke field per type.
        /// </summary>
        public CozySerializedValue LocalValue = new();
        public RuntimePortReference portReference;
        public RuntimeVariableReference variableReference;

        /// <summary> Defines the types of values a port can have. If a node is connect, value type will be port. </summary>
        public enum PortType { Value, Port, Variable, SpecialCode}

        /// <summary> Stores which value should be used. (What is connected to thi) </summary>
        public PortType type;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        public void SetValue(object value, Type declaredType = null)
        {
            if (value is RuntimePortReference reference)
            {
                type = PortType.Port;
                portReference = reference;
                ImportMessage = "Connected Port";
            }
            else if (value is RuntimeVariableReference referenceToVariable)
            {
                type = PortType.Variable;
                variableReference = referenceToVariable;
                ImportMessage = "Blackboard Variable";
            }
            else
            {
                type = PortType.Value;
                LocalValue ??= new CozySerializedValue();
                LocalValue.SetValue(value, declaredType);
                ImportMessage = value is Array ? "Array" : "Local Value";
            }
        }

        public void SetSpecialValue(int _code)
        { 
            
        }

        /// <summary> Reads the value if the port is not connected, evaluates the value if so. </summary>
        public object GetValue()
        {
            return LocalValue?.GetValue(GetValueType());
        }

        public Type GetValueType()
        {
            if (string.IsNullOrEmpty(ValueTypeName))
                return null;

            return Type.GetType(ValueTypeName);
        }
    }

    /// <summary>
    /// Points from an input port to the output port that supplies its value.
    /// The runtime resolves this reference only when a node asks for the input.
    /// </summary>
    [Serializable]
    public class RuntimePortReference
    {
        public string nodeID;
        public string portName;

        public RuntimePortReference(string sourceNodeID, string sourcePortName)
        {
            nodeID = sourceNodeID;
            portName = sourcePortName;
        }
    }

    /// <summary>
    /// Identifies a Blackboard variable without storing its current value in the
    /// imported graph asset. RuntimeCozyNode resolves this through the CozyManager
    /// that is currently running the graph.
    /// </summary>
    [Serializable]
    public class RuntimeVariableReference
    {
        public string variableID;

        public RuntimeVariableReference(string variableID)
        {
            this.variableID = variableID;
        }
    }

    [Serializable]
    public class RuntimeIVariable
    {
        public string nodeID;
        public string variableName;

        public RuntimeIVariable(string _nodeID, string _variableName)
        {
            nodeID = _nodeID;
            variableName = _variableName;
        }
    }

    [Serializable]
    public class RuntimeIConstant
    {
        public string nodeID;
        public string constantName;

        public RuntimeIConstant(string _nodeID, string _constantName)
        {
            nodeID = _nodeID;
            constantName = _constantName;
        }
    }
}
