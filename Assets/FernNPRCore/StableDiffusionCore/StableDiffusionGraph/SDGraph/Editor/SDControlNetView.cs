using System.Collections.Generic;
using BlueGraph;
using BlueGraph.Editor;
using StableDiffusionGraph.SDGraph.Nodes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StableDiffusionGraph.SDGraph.Editor
{
    [CustomNodeView(typeof(SDControlNet))]
    public class SDControlNetView : NodeView
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            PortView inView = GetInputPort("SDFlowIn");
            PortView outView = GetOutputPort("SDFlowOut");
            if (inView != null) inView.AddToClassList("SDFlowInPortView");
            if (outView != null) outView.AddToClassList("SDFlowOutPortView");

            var controlNet = Target as SDControlNet;
            if(controlNet == null) return;
            OnAsync();
            var button = new Button(OnAsync);
            button.style.backgroundImage = SDTextureHandle.RefreshIcon;
            button.style.width = 20;
            button.style.height = 20;
            button.style.alignSelf = Align.FlexEnd;
            button.style.bottom = 0;
            button.style.right = 0;
            mainContainer.Add(button);
            
            RefreshExpandedState();
        }
        
        private void OnAsync()
        {
            var controlNet = Target as SDControlNet;
            if(controlNet == null) return;
            extensionContainer.Clear();

            if (controlNet.modelList != null && controlNet.modelList.Count > 0)
            {
                // Create a VisualElement with a popup field
                var listContainer = new VisualElement();
                listContainer.style.flexDirection = FlexDirection.Row;
                listContainer.style.alignItems = Align.Center;
                listContainer.style.justifyContent = Justify.Center;
            
                var popup = new PopupField<string>(controlNet.modelList, controlNet.currentModelListIndex);
            
                // Add a callback to perform additional actions on value change
                popup.RegisterValueChangedCallback(evt =>
                {
                    controlNet.model = evt.newValue;
                    controlNet.currentModelListIndex = controlNet.modelList.IndexOf(evt.newValue);
                });

                listContainer.Add(popup);
                
                extensionContainer.Add(listContainer);
            }
            
            if (controlNet.moudleList != null && controlNet.moudleList.Count > 0)
            {
                // Create a VisualElement with a popup field
                var listContainer = new VisualElement();
                listContainer.style.flexDirection = FlexDirection.Row;
                listContainer.style.alignItems = Align.Center;
                listContainer.style.justifyContent = Justify.FlexStart;
            
                var popup = new PopupField<string>(controlNet.moudleList, controlNet.currentMoudleListIndex);
            
                // Add a callback to perform additional actions on value change
                popup.RegisterValueChangedCallback(evt =>
                {
                    controlNet.module = evt.newValue;
                    controlNet.currentMoudleListIndex = controlNet.moudleList.IndexOf(evt.newValue);
                });

                listContainer.Add(popup);
                
                extensionContainer.Add(listContainer);
            }
            
            RefreshExpandedState();
        }
    }
}
