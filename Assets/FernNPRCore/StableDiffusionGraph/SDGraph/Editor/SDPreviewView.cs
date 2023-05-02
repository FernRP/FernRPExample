using System;
using System.Collections.Generic;
using System.IO;
using FernGraph;
using FernGraph.Editor;
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
            PortView inView = GetInputPort("In Image");
            if (inView != null) inView.AddToClassList("PreviewInImg");
            
            style.transformOrigin = new TransformOrigin(0, 0);
            style.scale = new StyleScale(Vector3.one);
            style.maxWidth = 256;
            
            this.preview = Target as SDPreview;
            
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
                if (this.preview != null)
                {
                    OnUpdateAction(preview.Image);
                }
            });
            scaleListDropdown.style.width = StyleKeyword.Auto;

            var button = new Button(OnSave);
            button.style.backgroundImage = SDTextureHandle.SaveIcon;
            button.style.width = 20;
            button.style.height = 20;
            button.style.alignSelf = Align.FlexEnd;
            button.style.bottom = 0;
            button.style.right = 0;
            
            containerImageScale = new VisualElement();
            containerImageScale.style.flexDirection = FlexDirection.Row;
            containerImageScale.style.alignItems = Align.Center;
            containerImageScale.Add(imageScalelabel);
            containerImageScale.Add(scaleListDropdown);
            containerImageScale.Add(button);
            
            if (this.preview != null)
            {
                preview.OnUpdateAction = null;
                preview.OnUpdateAction += OnUpdateAction;
            }
            
            RefreshExpandedState();
        }

        private void OnSave()
        {
            this.preview = Target as SDPreview;
            if (this.preview != null)
            {
                if (preview.Image != null)
                {
                    string path = EditorUtility.SaveFilePanel("Save texture as PNG", "Assets", $"img_{preview.seed}.png", "png");
                    if (path.Length != 0)
                    {
                        SDUtil.SaveAsLinearPNG(preview.Image, path);
                        AssetDatabase.Refresh();
                        SDUtil.SetToNone(path);
                    }
                }
            }
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

                var previewVE = new Image();
                previewVE.scaleMode = ScaleMode.ScaleAndCrop;
                previewVE.image = preview.Image;

                if (previewVE.image != null)
                {
                    extensionContainer.Add(containerImageScale);
                    int scaleWidth = (int)((float)previewVE.image.width * scaleValue);
                    int scaleHeight = (int)((float)previewVE.image.height * scaleValue);
                    style.maxWidth = 256 + scaleWidth;
                    style.maxHeight = 256 + scaleHeight;
                    previewVE.style.maxWidth = scaleWidth;
                    previewVE.style.maxHeight = scaleHeight;
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
