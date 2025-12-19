using Codice.CM.Common.Tree;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ShaderFactory.CozyGraphToolkit.Runtime
{
    /// <summary>
    /// Serializable port key/value pair with typed fields
    /// </summary>
    [Serializable]
    public class CozyRuntimePort
    {
        public string ImportMessage;

        public string Key;

        private RuntimeCozyNode node;

        // UNITY-SERIALIZABLE TYPE FIELDS  
        public string StringValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
        public RuntimeIPort PortValue;

        /// <summary> Defines the types of values a port can have. If a node is connect, value type will be port. </summary>
        public enum PortType { String, Float, Int, Bool, Port, SpecialCode}

        /// <summary> Stores which value should be used. (What is connected to thi) </summary>
        public PortType Type;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        public void SetValue (object _value)
        {
            if (_value is float f)
            {
                Type = PortType.Float;
                FloatValue = f;
                ImportMessage = "Float";
            }
            else if (_value is int i)
            {
                Type = PortType.Int;
                IntValue = i;
                ImportMessage = "Integer";
            }
            else if (_value is bool b)
            {
                Type = PortType.Bool;
                BoolValue = b;
                ImportMessage = "Boolean";
            }
            else if (_value is string s)
            {
                Type = PortType.String;
                StringValue = s;
                ImportMessage = "String";
            }
            else if (_value is RuntimeIPort rip)
            {
                Type = PortType.Port;
                PortValue = new RuntimeIPort(rip.nodeID, rip.portName);
                ImportMessage = "Connected Node";
            }
            else if (_value is RuntimeIVariable riv)
            {
                Type = PortType.Port;
                PortValue = new RuntimeIPort(riv.nodeID, riv.variableName);
                ImportMessage = "Variable (Connected Node)";
            }
            else if (_value is RuntimeIConstant ric)
            {
                Type = PortType.Port;
                PortValue = new RuntimeIPort(ric.nodeID, ric.constantName);
                ImportMessage = "Constant (Connected Node)";
            }
            else if (_value is null)
            {
                ImportMessage = "Import Failed!";
            }
        }

        /// <summary>
        /// Reads the value if the port is not connected, evaluates the value if so.
        /// </summary>
        public object GetValue()
        {
            return Type switch
            {
                PortType.Float => FloatValue,
                PortType.Int => IntValue,
                PortType.String => StringValue,
                PortType.Bool => BoolValue,
                PortType.Port => EvaluateConnectedPort(),
                _ => null
            };
        }

        private object EvaluateConnectedPort()
        {
            // var result = PortValue.node
            return "PORT, CAN'T RETRIEVE VALUE YET";
        }
    }

    [Serializable]
    public class RuntimeIPort
    {
        public string nodeID;
        public string portName;
        public RuntimeCozyNode node;

        public RuntimeIPort(string _nodeID, string _portName)
        {
            nodeID = _nodeID;
            portName = _portName;
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