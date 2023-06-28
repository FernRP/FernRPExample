using System.Collections.Generic;
using FernGraph;
using FernGraph.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FernNPRCore.StableDiffusionGraph
{
    [CustomNodeView(typeof(SDImg2ImgNode))]
    public class SDImg2ImgNodeView : NodeView
    {
        bool foldout = false;
        private LongField longField;
        private LongField longLastField;
        protected override void OnInitialize()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            PortView inView = GetInputPort("SDFlowIn");
            PortView outView = GetOutputPort("SDFlowOut");
            if (inView != null) inView.AddToClassList("SDFlowInPortView");
            if (outView != null) outView.AddToClassList("SDFlowOutPortView");
            
            var samplerNode = Target as SDImg2ImgNode;
            if(samplerNode == null) return;

            samplerNode.OnUpdateSeedField = null;
            samplerNode.OnUpdateSeedField += OnUpadteSeed;

            List<string> samplerMethodList = new List<string>();
            samplerMethodList.AddRange(SDDataHandle.Instance.samplers);

            var samplerMethodDropdown = new DropdownField(samplerMethodList, 0);
            samplerMethodDropdown.RegisterValueChangedCallback(e =>
            {
                samplerNode.SamplerMethod = e.newValue;
            });
            samplerMethodDropdown.style.flexGrow = 1;
            samplerMethodDropdown.style.maxWidth = 140;
            
            var label = new Label("Method    ");
            label.style.width = StyleKeyword.Auto;
            label.style.marginRight = 5;
            
            var containerSampleMethod = new VisualElement();
            containerSampleMethod.style.flexDirection = FlexDirection.Row;
            containerSampleMethod.style.alignItems = Align.Center;
            containerSampleMethod.Add(label);
            containerSampleMethod.Add(samplerMethodDropdown);
            
            extensionContainer.Add(containerSampleMethod);
            
            // seed
            var labelSeed = new Label("Seed        ");
            labelSeed.style.width = StyleKeyword.Auto;
            labelSeed.style.marginRight = 5;
            
            longField = new LongField();
            longField.value = -1;
            longField.RegisterValueChangedCallback((e) =>
            {
                samplerNode.Seed = e.newValue;
            });
            
            longField.style.flexGrow = 1;
            longField.style.maxWidth = 140;
            var containerSeed = new VisualElement();
            containerSeed.style.flexDirection = FlexDirection.Row;
            containerSeed.style.alignItems = Align.Center;
            containerSeed.Add(labelSeed);
            containerSeed.Add(longField);
            extensionContainer.Add(containerSeed);
            
            // last seed
            var labelLastSeed = new Label("Last Seed");
            labelLastSeed.style.width = StyleKeyword.Auto;
            labelLastSeed.style.marginRight = 5;
            
            longLastField = new LongField();
            longLastField.value = samplerNode.outSeed;
            longLastField.style.flexGrow = 1;
            longLastField.style.maxWidth = 140;
            var containerLastSeed = new VisualElement();
            containerLastSeed.style.flexDirection = FlexDirection.Row;
            containerLastSeed.style.alignItems = Align.Center;
            containerLastSeed.Add(labelLastSeed);
            containerLastSeed.Add(longLastField);
            extensionContainer.Add(containerLastSeed);
            
            var container = new IMGUIContainer(OnGUI);
            extensionContainer.Add(container);
            
            RefreshExpandedState();
        }
        

        private void OnGUI()
        {
            var imgNode = Target as SDImg2ImgNode;
            if(imgNode == null) return;
            var styleTextArea = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };
           
            var styleCheckbox = new GUIStyle(EditorStyles.toggle);
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "InPaint");
            if (foldout)
            {
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("InPaint Fill", GUILayout.MaxWidth(150));
                imgNode.inpainting_fill = EditorGUILayout.IntField(
                    imgNode.inpainting_fill, 
                    styleTextArea,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Inpaint Full Res", GUILayout.MaxWidth(150));
                imgNode.inpaint_full_res = EditorGUILayout.Toggle(
                    imgNode.inpaint_full_res, 
                    styleCheckbox,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Inpaint Full Res Padding", GUILayout.MaxWidth(150));
                imgNode.inpaint_full_res_padding = EditorGUILayout.IntField(
                    imgNode.inpaint_full_res_padding, 
                    styleTextArea,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Inpaint Mask Invert", GUILayout.MaxWidth(150));
                imgNode.inpainting_mask_invert = EditorGUILayout.IntField(
                    imgNode.inpainting_mask_invert, 
                    styleTextArea,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mask Blur", GUILayout.MaxWidth(150));
                imgNode.mask_blur = EditorGUILayout.IntField(
                    imgNode.mask_blur, 
                    styleTextArea,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void OnUpadteSeed(long seed, long outSeed)
        {
            var samplerNode = Target as SDImg2ImgNode;
            if(samplerNode == null) return;
            samplerNode.Seed = seed;
            samplerNode.outSeed = outSeed;
            longField.value = seed;
            longLastField.value = outSeed;
        }
    }
}
