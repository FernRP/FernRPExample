using System;
using System.Collections.Generic;
using System.IO;
using FernGraph;
using FernGraph.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace FernNPRCore.StableDiffusionGraph
{
    [CustomNodeView(typeof(SDCameraCapture))]
    public class SDCameraCaptureView : NodeView
    {
        private SDCameraCapture capture;
        private float scaleValue = 0.25f;
        List<string> ScaleList = new List<string>();
        private VisualElement containerImageScale;
        private Image previewVE = new Image();
        private Toggle enableUpdateCheck = new Toggle();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            PortView inView = GetInputPort("In Image");
            if (inView != null) inView.AddToClassList("PreviewInImg");
            
            style.transformOrigin = new TransformOrigin(0, 0);
            style.scale = new StyleScale(Vector3.one);
            style.maxWidth = 256;
            
            ScaleList.Clear();
            foreach (var scale in SDUtil.ScaleList)
            {
                ScaleList.Add($"{scale}%");
            }
            
            var imageScalelabel = new Label("Image Scale");
            imageScalelabel.style.width = StyleKeyword.Auto;
            imageScalelabel.style.marginRight = 5;
            
            var scaleListDropdown = new DropdownField(ScaleList, 0);
            scaleListDropdown.RegisterValueChangedCallback(e =>
            {
                scaleValue = SDUtil.ScaleList[ScaleList.IndexOf(e.newValue)] / 100.0f;
            });
            scaleListDropdown.style.width = StyleKeyword.Auto;
            containerImageScale = new VisualElement();
            containerImageScale.style.flexDirection = FlexDirection.Row;
            containerImageScale.style.alignItems = Align.Center;
            containerImageScale.Add(imageScalelabel);
            containerImageScale.Add(scaleListDropdown);

            capture = Target as SDCameraCapture;
            if (capture != null)
            {
                enableUpdateCheck = new Toggle();
                enableUpdateCheck.RegisterValueChangedCallback(e =>
                {
                    capture.enableUpdate = e.newValue;
                });
            
                // capture update event
                capture.OnUpdateTexture = null;
                capture.OnUpdateTexture += OnUpdateAction;
            }

            

            RefreshExpandedState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            capture.OnUpdateTexture = null;
        }
        
        private void OnUpdateAction(RenderTexture cameraRT)
        {
            if(capture == null) return;
            
            if (cameraRT != null)
            {
                extensionContainer.Clear();
                
                previewVE.scaleMode = ScaleMode.ScaleAndCrop;
                previewVE.image = capture.cameraRT;

                if (previewVE.image != null)
                {
                    extensionContainer.Add(containerImageScale);
                    extensionContainer.Add(enableUpdateCheck);
                    int scaleWidth = (int)((float)previewVE.image.width * scaleValue);
                    int scaleHeight = (int)((float)previewVE.image.height * scaleValue);
                    style.maxWidth = 256 + scaleWidth;
                    style.maxHeight = 256 + scaleHeight;
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
}
