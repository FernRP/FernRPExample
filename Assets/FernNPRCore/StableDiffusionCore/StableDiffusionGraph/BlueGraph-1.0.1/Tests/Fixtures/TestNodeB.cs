using UnityEngine;

namespace BlueGraph.Tests
{
    public class TestNodeB : Node
    {
        [Input("Input")]
        public Vector3 bValue1;

        [Output("Output")]
        public string bValue2;
    
        public TestNodeB() : base()
        {
            Name = "Test Node B";

            AddPort(new Port
            { 
                Name = "Input",
                Direction = PortDirection.Input,
                Type = typeof(Vector3) 
            });

            AddPort(new Port
            { 
                Name = "Output",
                Direction = PortDirection.Output,
                Type = typeof(string) 
            });
        }
        
        public override object OnRequestValue(Port port)
        {
            throw new System.NotImplementedException();
        }
    }
}
