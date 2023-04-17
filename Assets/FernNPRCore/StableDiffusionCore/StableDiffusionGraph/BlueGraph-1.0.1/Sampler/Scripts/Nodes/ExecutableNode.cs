using UnityEngine;
using BlueGraph;
using System.Linq;

namespace BlueGraphSamples
{
    /// <summary>
    /// Node that exposes an execution port for both IO. 
    /// 
    /// Inherit to make a node executable for forward execution. 
    /// </summary>
    [Output("ExecOut", typeof(ExecutionFlowData), Multiple = false)]
    public abstract class ExecutableNode : Node, IExecutableNode
    {
        [Input("ExecIn", Multiple = true)] public ExecutionFlowData execIn;
        
        public override object OnRequestValue(Port port) => null;

        /// <summary>
        /// Execute this node and return the next node to be executed.
        /// Override with your custom execution logic. 
        /// </summary>
        /// <returns></returns>
        public virtual IExecutableNode Execute(ExecutionFlowData data)
        {
            // noop.
            return GetNextExecutableNode();
        }

        /// <summary>
        /// Get the next node that should be executed along the edge
        /// </summary>
        /// <returns></returns>
        public IExecutableNode GetNextExecutableNode(string portName = "ExecOut")
        {
            var port = GetPort(portName);
            if (port.ConnectionCount < 1) 
            {
                return null;
            }
            
            var node = port.ConnectedPorts.First()?.Node;
            if (node is IExecutableNode execNode)
            {
                return execNode;
            }

            Debug.LogWarning(
                $"<b>[{Name}]</b> Connected output node {node.Name} to port {port.Name} is not an ICanExec. " +
                $"Cannot execute past this point."
            );

            return null;
        }
    }
}
