using System;
using System.Collections.Generic;
using BlueGraph;
using BlueGraph.Editor;
using BlueGraphSamples;
using StableDiffusionGraph.SDGraph.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace StableDiffusionGraph.SDGraph.Editor
{
    [CustomNodeView(typeof(SDPreview))]
    public class SDPreviewView : NodeView
    {
        private SDPreview preview;
        private float scaleValue = 0.2f;
        List<string> ScaleList = new List<string>();
        private VisualElement containerImageScale;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            PortView inView = GetInputPort("SDFlowIn");
            PortView outView = GetOutputPort("SDFlowOut");
            if (inView != null) inView.AddToClassList("SDFlowInPortView");
            if (outView != null) outView.AddToClassList("SDFlowOutPortView");
            
            style.transformOrigin = new TransformOrigin(0, 0);
            style.scale = new StyleScale(Vector3.one);
            
           
            this.preview = Target as SDPreview;
            
            ScaleList.Clear();
            foreach (var scale in SDUtil.ScaleList)
            {
                ScaleList.Add($"{scale}%");
            }
            
            var scaleListDropdown = new DropdownField(ScaleList, 0);
            scaleListDropdown.RegisterValueChangedCallback(e =>
            {
                scaleValue = SDUtil.ScaleList[ScaleList.IndexOf(e.newValue)] / 100.0f;
                if (this.preview != null)
                {
                    OnUpdateAction(preview.Image);
                }
            });
            scaleListDropdown.style.width = StyleKeyword.Auto;
            
            var imageScalelabel = new Label("Image Scale");
            imageScalelabel.style.width = StyleKeyword.Auto;
            imageScalelabel.style.marginRight = 5;
            
            containerImageScale = new VisualElement();
            containerImageScale.style.flexDirection = FlexDirection.Row;
            containerImageScale.style.alignItems = Align.Center;
            containerImageScale.Add(imageScalelabel);
            containerImageScale.Add(scaleListDropdown);
            
            if (this.preview != null)
            {
                preview.OnUpdateAction = null;
                preview.OnUpdateAction += OnUpdateAction;
            }
            
            RefreshExpandedState();
        }

        public override void OnDirty()
        {
            base.OnDirty();
            this.preview = Target as SDPreview;
            if (this.preview != null)
            {
                OnUpdateAction(preview.Image);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            this.preview = Target as SDPreview;
            if (this.preview != null)
            {
                OnUpdateAction(preview.Image);
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            this.preview = Target as SDPreview;
            if (this.preview != null)
            {
                OnUpdateAction(preview.Image);
            }
        }
        
        

        private void OnUpdateAction(Texture2D obj)
        {
            if(preview == null) return;
            
            if (preview.Image != null)
            {
                extensionContainer.Clear();
                
                extensionContainer.Add(containerImageScale);

                var previewVE = new Image();
                previewVE.scaleMode = ScaleMode.ScaleAndCrop;
                previewVE.image = preview.Image;
                int scaleWidth = (int)((float)previewVE.image.width * scaleValue);
                int scaleHeight = (int)((float)previewVE.image.height * scaleValue);
                Debug.Log(scaleValue);
                Debug.Log("scale w: " + scaleWidth + " h: " + scaleHeight);
                style.maxWidth = 64 + scaleWidth;
                style.maxHeight = 111 + scaleHeight;
                
                previewVE.style.maxWidth = scaleWidth;
                previewVE.style.maxWidth = scaleHeight;
                var asptio = (float)previewVE.image.width / (float)scaleWidth;
                previewVE.style.maxHeight = previewVE.image.height / asptio;
                previewVE.AddToClassList("previewVE");
                extensionContainer.Add(previewVE);
                RefreshExpandedState();
            }
        }
    }
}
