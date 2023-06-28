using FernGraph;
using FernGraph.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FernNPRCore.StableDiffusionGraph
{
    [CustomNodeView(typeof(SDFlowNode))]
    public class SDNodeView : NodeView
    {
        protected override void OnInitialize()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            PortView inView = GetInputPort("SDFlowIn");
            PortView outView = GetOutputPort("SDFlowOut");
            if (inView != null) inView.AddToClassList("SDFlowInPortView");
            if (outView != null) outView.AddToClassList("SDFlowOutPortView");
        }
    }
}
