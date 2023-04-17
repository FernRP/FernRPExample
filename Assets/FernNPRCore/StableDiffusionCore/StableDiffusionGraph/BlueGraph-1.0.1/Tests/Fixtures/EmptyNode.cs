namespace BlueGraph.Tests
{
    /// <summary>
    /// Node without any ports or fields to test with
    /// </summary>
    public class EmptyNode : Node
    {
        public EmptyNode() : base()
        {
            Name = "Empty Node";
        }

        public override object OnRequestValue(Port port)
        {
            throw new System.NotImplementedException();
        }
    }
}
