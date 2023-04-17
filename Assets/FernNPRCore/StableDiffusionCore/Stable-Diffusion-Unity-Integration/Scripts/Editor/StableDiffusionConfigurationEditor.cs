using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector to add a button to seek the models list 
/// from a StableDiffusionConfiguration.
/// </summary>
[CustomEditor(typeof(StableDiffusionConfiguration))]
public class StableDiffusionConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        StableDiffusionConfiguration myComponent = (StableDiffusionConfiguration)target;

        if (GUILayout.Button("List Models"))
            myComponent.ListModels();
    }
}