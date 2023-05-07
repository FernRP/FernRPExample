using FernGraph;
using FernGraph.Editor;
using StableDiffusionGraph.SDGraph.Nodes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace StableDiffusionGraph.SDGraph.Editor
{
    [CustomNodeView(typeof(SDStart))]
    public class SDStartView : NodeView
    {
        bool foldout = false;
        protected override void OnInitialize()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("SDGraphRes/SDNodeView"));
            AddToClassList("sdNodeView");
            if (Target is SDStart)
            {
                AddToClassList("sdConfigView");
            }
            PortView inView = GetInputPort("SDFlowIn");
            PortView outView = GetOutputPort("SDFlowOut");
            if (inView != null) inView.AddToClassList("SDFlowInPortView");
            if (outView != null) outView.AddToClassList("SDFlowOutPortView");
            
            // Setup a container to render IMGUI content in 
            var container = new IMGUIContainer(OnGUI);
            extensionContainer.Add(container);
            
            RefreshExpandedState();
        }

        void OnGUI()
        {
            var config = Target as SDStart;
            if(config == null) return;
            var styleTextArea = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true
            };
            var styleCheckbox = new GUIStyle(EditorStyles.toggle);

            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Config");

            if (foldout)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Server URL", GUILayout.MaxWidth(80));
                config.serverURL = EditorGUILayout.TextArea(
                    config.serverURL, 
                    styleTextArea,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Use Auth", GUILayout.MaxWidth(80));
                config.useAuth = EditorGUILayout.Toggle(
                    config.useAuth, 
                    styleCheckbox,
                    GUILayout.MaxWidth(150)
                );
                EditorGUILayout.EndHorizontal();
                if (config.useAuth)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("UserName", GUILayout.MaxWidth(80));
                    config.user = EditorGUILayout.TextArea(
                        config.user, 
                        styleTextArea,
                        GUILayout.MaxWidth(150)
                    );
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Password", GUILayout.MaxWidth(80));
                    config.pass = EditorGUILayout.TextArea(
                        config.pass, 
                        styleTextArea,
                        GUILayout.MaxWidth(150)
                    );
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
