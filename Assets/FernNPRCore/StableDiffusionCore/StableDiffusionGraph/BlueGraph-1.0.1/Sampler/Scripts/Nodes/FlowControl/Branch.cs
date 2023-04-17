using UnityEngine;
using BlueGraph;

namespace BlueGraphSamples
{
    /// <summary>
    /// Example of an execution node representing <c>if (cond) { ... } else { ... }</c>
    /// </summary>
    [Node(Path = "Flow Control")]
    [Tags("Flow Control")]
    [Output("Else", typeof(ExecutionFlowData), Multiple = false)]
    public class Branch : ExecutableNode
    {
        [Input("Condition")] public bool condition;

        public override IExecutableNode Execute(ExecutionFlowData data)
        {
            bool condition = GetInputValue("Condition", this.condition);
            if (!condition)
            {
                return GetNextExecutableNode("Else");
            }
            
            // True (default) case
            return base.Execute(data);
        }
    }
}
