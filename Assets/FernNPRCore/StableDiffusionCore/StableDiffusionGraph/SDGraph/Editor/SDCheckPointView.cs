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
    [CustomNodeView(typeof(SDCheckPoint))]
    public class SDCheckPointView : NodeView
    {
        private string[] modelNames;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            var checkPoint = Target as SDCheckPoint;
            if(checkPoint == null) return;
            extensionContainer.Clear();
            OnAsync();
            var button = new Button(OnAsync);
            button.style.backgroundImage = SDTextureHandle.RefreshIcon;
            button.style.width = 20;
            button.style.height = 20;
            button.style.alignSelf = Align.FlexEnd;
            button.style.bottom = 0;
            button.style.right = 0;
            titleButtonContainer.Add(button);
            
            RefreshExpandedState();
        }

        private void OnAsync()
        {
            var checkPoint = Target as SDCheckPoint;
            if(checkPoint == null) return;
            modelNames = checkPoint.modelNames;
            if (modelNames != null && modelNames.Length > 0)
            {
                extensionContainer.Clear();
                // Create a VisualElement with a popup field
                var listContainer = new VisualElement();
                listContainer.style.flexDirection = FlexDirection.Row;
                listContainer.style.alignItems = Align.Center;
                listContainer.style.justifyContent = Justify.Center;
            
                List<string> stringList = new List<string>();
                stringList.AddRange(checkPoint.modelNames);
                var popup = new PopupField<string>(stringList, checkPoint.currentIndex);
            
                // Add a callback to perform additional actions on value change
                popup.RegisterValueChangedCallback(evt =>
                {
                    Debug.Log("Selected item: " + evt.newValue);
                    checkPoint.Model = evt.newValue;
                    checkPoint.currentIndex = stringList.IndexOf(evt.newValue);
                });

                listContainer.Add(popup);
                
                extensionContainer.Add(listContainer);
                RefreshExpandedState();
            }
        }
    }
}
