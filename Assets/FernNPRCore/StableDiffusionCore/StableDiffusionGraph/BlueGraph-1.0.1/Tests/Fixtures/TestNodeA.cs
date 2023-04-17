namespace BlueGraph.Tests
{
    public class TestNodeA : Node
    {
        [Input("Input")]
        public int aValue1 = 5;

        [Output("Output")]
        public int aValue2;
    
        public TestNodeA() : base()
        {
            Name = "Test Node A";

            AddPort(new Port
            { 
                Name = "Input",
                Direction = PortDirection.Input,
                Type = typeof(int) 
            });
            
            AddPort(new Port
            { 
                Name = "Output",
                Direction = PortDirection.Output,
                Type = typeof(int),
                Capacity = PortCapacity.Multiple
            });
        }

        /// <summary>
        /// Simply increments the input value by one
        /// </summary>
        public override object OnRequestValue(Port port)
        {
            var a = GetInputValue("Input", aValue1);
            return a + 1;
        }
    }

    public class InheritedTestNodeA : TestNodeA { }
}
