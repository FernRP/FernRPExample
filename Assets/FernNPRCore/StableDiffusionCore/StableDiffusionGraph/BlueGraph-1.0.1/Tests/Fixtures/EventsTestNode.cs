namespace BlueGraph.Tests
{
    /// <summary>
    /// Test node that tracks what events were fired during a test
    /// </summary>
    public class EventTestNode : Node
    {
        public int onEnableCount = 0;
        public int onDisableCount = 0;
    
        public EventTestNode() : base()
        {
            Name = "Test Node B";
            
            AddPort(new InputPort<float> { Name = "Input" });
            AddPort(new OutputPort<float> { Name = "Output" });
        }

        public override void OnEnable()
        {
            onEnableCount++;
            base.OnEnable();
        }

        public override void OnDisable()
        {
            onDisableCount++;
            base.OnDisable();
        }

        public override object OnRequestValue(Port port)
        {
            throw new System.NotImplementedException();
        }
    }
}
