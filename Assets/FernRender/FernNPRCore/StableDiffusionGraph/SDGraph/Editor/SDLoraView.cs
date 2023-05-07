using System.Collections.Generic;
using FernGraph;
using FernGraph.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FernNPRCore.StableDiffusionGraph
{
    [CustomNodeView(typeof(SDLora))]
    public class SDLoraView : NodeView
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();

            var lora = Target as SDLora;
            if(lora == null) return;
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
            var lora = Target as SDLora;
            if(lora == null) return;
            if (lora.loraNames != null && lora.loraNames.Count > 0)
            {
                extensionContainer.Clear();
                // Create a VisualElement with a popup field
                var listContainer = new VisualElement();
                listContainer.style.flexDirection = FlexDirection.Row;
                listContainer.style.alignItems = Align.Center;
                listContainer.style.justifyContent = Justify.Center;
            
                var popup = new PopupField<string>(lora.loraNames, lora.currentIndex);
            
                // Add a callback to perform additional actions on value change
                popup.RegisterValueChangedCallback(evt =>
                {
                    Debug.Log("Selected item: " + evt.newValue);
                    lora.lora = evt.newValue;
                    lora.currentIndex = lora.loraNames.IndexOf(evt.newValue);
                });

                listContainer.Add(popup);
                
                extensionContainer.Add(listContainer);
                RefreshExpandedState();
            }
        }
    }
}
