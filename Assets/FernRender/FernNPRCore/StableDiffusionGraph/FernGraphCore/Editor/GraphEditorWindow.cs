using System;
using PlasticGui.WorkspaceWindow.Items;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FernGraph.Editor
{
    /// <summary>
    /// Build a basic window container for the FernGraph canvas
    /// </summary>
    public class GraphEditorWindow : EditorWindow
    {
        public CanvasView Canvas { get; protected set; }

        public Graph Graph { get; protected set; }

        private void OnInspectorUpdate()
        {
            Graph.Update();
        }

        /// <summary>
        /// Load a graph asset in this window for editing
        /// </summary>
        public virtual void Load(Graph graph)
        {
            Graph = graph;

            Canvas = new CanvasView(this);
            Canvas.Load(graph);
            Canvas.StretchToParentSize();
            
            // Create a new panel to hold the button
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            panel.style.marginLeft = 5;
            panel.style.marginTop = 5;
            panel.style.width = 120;
            panel.style.height = 200;
            panel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            panel.style.justifyContent = Justify.Center;
            panel.style.backgroundColor = new Color(0, 0, 0, 0.5f);

            var button = new Button(graph.ExecuteGraph);
            button.text = "Execute";
            button.style.width = 80;
            button.style.height = 30;
            button.style.marginTop = 20;
            
            panel.Add(button);
            
            // Create a new VisualElement to wrap the foldout
            var foldoutWrapper = new VisualElement();
            // Apply the background color and opacity to the wrapper
            foldoutWrapper.style.backgroundColor = new Color(0, 0, 0, 0.5f);
            // Set the width and max width of the wrapper
            foldoutWrapper.style.width = 130;
            foldoutWrapper.style.maxWidth = 130;
            
            // Set the initial position of the wrapper
            foldoutWrapper.style.position = new StyleEnum<Position>(Position.Absolute);
            foldoutWrapper.style.left = Canvas.layout.width - foldoutWrapper.layout.width - 5;
            foldoutWrapper.style.top = 5;
            
            // Make the wrapper draggable
            foldoutWrapper.RegisterCallback<MouseDownEvent>(evt => {
                evt.StopPropagation();
                foldoutWrapper.CaptureMouse();
                foldoutWrapper.userData = evt.localMousePosition; // save the position of the mouse
            });
            foldoutWrapper.RegisterCallback<MouseMoveEvent>(evt => {
                if (foldoutWrapper.HasMouseCapture())
                {
                    // calculate the new position of the wrapper
                    var delta = evt.localMousePosition - (Vector2)foldoutWrapper.userData;
                    var newPosition = foldoutWrapper.layout.position + delta;

                    // update the style of the wrapper with the new position
                    foldoutWrapper.style.position = new StyleEnum<Position>(Position.Absolute);
                    foldoutWrapper.style.left = newPosition.x;
                    foldoutWrapper.style.top = newPosition.y;
                }
            });
            foldoutWrapper.RegisterCallback<MouseUpEvent>(evt => {
                foldoutWrapper.ReleaseMouse();
            });
            
            foldoutWrapper.Add(panel);
            
           
            
            Canvas.Add(foldoutWrapper);
            
            rootVisualElement.Add(Canvas);
            var btn = new Button();

            titleContent = new GUIContent(graph.name);
            Repaint();
        }

        protected virtual void Update()
        {
            // Canvas can be invalidated when the Unity Editor
            // is closed and reopened with this editor window persisted.
            if (Canvas == null)
            {
                Close();
                return;
            }

            Canvas.Update();
        }

        /// <summary>
        /// Restore an already opened graph after a reload of assemblies
        /// </summary>
        protected virtual void OnEnable()
        {
            if (Graph)
            {
                Load(Graph);
            }
        }
    }
}
