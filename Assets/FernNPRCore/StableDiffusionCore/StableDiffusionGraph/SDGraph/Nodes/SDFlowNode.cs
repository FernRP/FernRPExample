using System.Collections;
using System.Linq;
using BlueGraph;
using UnityEngine;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Output("SDFlowOut", typeof(SDFlowData), Multiple = false)]
    public abstract class SDFlowNode : Node, ICanExecuteSDFlow
    {
        [Input("SDFlowIn", Multiple = true)] public SDFlowData data;
        
        public override object OnRequestValue(Port port) => null;

        public abstract IEnumerator Execute();

        public virtual ICanExecuteSDFlow GetNext()
        {
            var port = GetPort("SDFlowOut");
            return port.ConnectedPorts.FirstOrDefault()?.Node as ICanExecuteSDFlow;
        }
    }
}
