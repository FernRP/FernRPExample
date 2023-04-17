using System.Reflection;

namespace BlueGraph.Tests
{
    public class InputPort<T> : Port { } 

    public class OutputPort<T> : Port { } 

    public class FuncNode : Node
    {
        public FuncNode(MethodInfo mi) { }

        public override object OnRequestValue(Port port)
        {
            throw new System.NotImplementedException();
        }
    }
}
