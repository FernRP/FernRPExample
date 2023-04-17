namespace BlueGraphSamples
{
    /// <summary>
    /// Interface for a node with one or more ExecutionFlowData ports 
    /// </summary>
    public interface IExecutableNode
    {
        /// <summary>
        /// Execute this node and return the next node to be executed.
        /// Override with your custom execution logic. 
        /// </summary>
        IExecutableNode Execute(ExecutionFlowData data);
    }
}
