using UnityEngine;

namespace BlueGraph.Tests
{
    public interface ITestClass
    {

    }

    public class BaseTestClass : ITestClass
    {
        public float value1;
        public float value2;
    }

    public class TestClass : BaseTestClass
    {
        public float value3;
    }

    public struct TestStruct
    {
        public float value1;
        public float value2;
    }

    /// <summary>
    /// Test fixture with a bunch of type options to check GetInputValue calls
    /// </summary>
    public class TypeTestNode : Node
    {
        public int intValue;
        public bool boolValue;
        public string stringValue;
        public float floatValue;
        public Vector3 vector3Value;
        public AnimationCurve curveValue;

        public TestClass testClassValue = new TestClass();
        public TestStruct testStructValue;
    
        public TypeTestNode() : base()
        {
            Name = "Type Test Node";
            
            // Input (any)
            AddPort(new Port { Name = "Input", Direction = PortDirection.Input });

            // Output types
            AddPort(new Port { Name = "intval", Direction = PortDirection.Output });
            AddPort(new Port { Name = "boolval", Direction = PortDirection.Output });
            AddPort(new Port { Name = "stringval", Direction = PortDirection.Output });
            AddPort(new Port { Name = "floatval", Direction = PortDirection.Output });
            AddPort(new Port { Name = "vector3val", Direction = PortDirection.Output });
            AddPort(new Port { Name = "curveval", Direction = PortDirection.Output });
            AddPort(new Port { Name = "classval", Direction = PortDirection.Output });
            AddPort(new Port { Name = "structval", Direction = PortDirection.Output });
        }

        public override object OnRequestValue(Port port)
        {
            switch (port.Name) 
            {
                case "intval": return intValue;
                case "boolval": return boolValue;
                case "stringval": return stringValue;
                case "vector3val": return vector3Value;
                case "curveval": return curveValue;
                case "classval": return testClassValue;
                case "structval": return testStructValue;
                default: return null;
            }
        }
    }
}
