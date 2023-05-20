using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace FernGraph.Editor
{
    /// <summary>
    /// Custom asset handler callback to react to "open" events in the Unity Editor
    /// </summary>
    public class GraphAssetHandler
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is Graph graph)
            {
                OnOpenGraph(graph);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Open the appropriate GraphEditor for the Graph asset
        /// </summary>
        public static void OnOpenGraph(Graph graph)
        {
            var editor = UnityEditor.Editor.CreateEditor(graph) as GraphEditor;
            if (!editor)
            {
                Debug.LogWarning("No editor found for graph asset");
            } 
            else
            {
                editor.CreateOrFocusEditorWindow();
            }
        }
    }
}
