using FernGraph;
using FernGraph.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FernNPRCore.StableDiffusionGraph
{
    [CustomNodeView(typeof(StringNode))]
    public class StringNodeView : NodeView
    {
        protected override void OnInitialize()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("stringNodeView");
            
            // Setup a container to render IMGUI content in 
            var container = new IMGUIContainer(OnGUI);
            extensionContainer.Add(container);
            
            RefreshExpandedState();
        }
        
        void OnGUI()
        {
            var text = Target as StringNode;
            if(text == null) return;
            var styleTextArea = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
            };
            EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                    text.Text = EditorGUILayout.TextArea(
                        text.Text, 
                        styleTextArea,
                        GUILayout.MaxWidth(300),
                        GUILayout.MinWidth(120),
                        GUILayout.ExpandHeight(true)
                    );
                EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
