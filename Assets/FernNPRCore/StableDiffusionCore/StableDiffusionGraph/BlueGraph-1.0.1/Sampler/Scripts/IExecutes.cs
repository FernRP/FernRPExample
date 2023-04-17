namespace BlueGraphSamples
{
    /// <summary>
    /// Interface for an entity that can execute a tree of ICanExecute nodes
    /// until the full depth has been traversed.
    /// </summary>
    public interface IExecutes
    {
        /// <summary>
        /// Execute the parent node and its outputs
        /// </summary>
        void Execute(IExecutableNode root, ExecutionFlowData data);
    }
}
