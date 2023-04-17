
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector to add a menu in the Assets/Create drop-down for a SDSettings.
/// </summary>
[CustomEditor(typeof(SDSettings))]
public class SDSettingsEditor : Editor
{
    [MenuItem("Assets/Create/SDSettings")]
    public static void CreateMyScriptableObject()
    {
        SDSettings asset = ScriptableObject.CreateInstance<SDSettings>();
        AssetDatabase.CreateAsset(asset, "Assets/NewSDSettings.asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}