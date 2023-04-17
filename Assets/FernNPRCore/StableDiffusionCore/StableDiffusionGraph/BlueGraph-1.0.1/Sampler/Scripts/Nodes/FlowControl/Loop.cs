using UnityEngine;
using BlueGraph;

namespace BlueGraphSamples
{
    /// <summary>
    /// Example of an execution node representing <c>for (i = 0 to count) { ... }</c>
    /// </summary>
    [Node(Path = "Flow Control")]
    [Tags("Flow Control")]
    [Output("Then", typeof(ExecutionFlowData), Multiple = false)]
    public class Loop : ExecutableNode
    {
        [Input("Count")] public int count;
        
        [Output("Current")] private int current;

        public override IExecutableNode Execute(ExecutionFlowData data)
        {
            int count = GetInputValue("Count", this.count);
            
            // Execution does not leave this node until the loop completes
            IExecutableNode next = GetNextExecutableNode();
            for (current = 0; current < count; current++)
            {
                (Graph as MonoBehaviourGraph).Execute(next, data);
            }
            
            return GetNextExecutableNode("Then");
        }

        public override object OnRequestValue(Port port)
        {
            if (port.Name == "Current")
            {
                return current;
            }

            return base.OnRequestValue(port);
        }
    }
}
