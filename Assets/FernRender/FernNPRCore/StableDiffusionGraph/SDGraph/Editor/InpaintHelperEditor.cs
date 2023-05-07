using System;
using System.Collections.Generic;
using System.Linq;
using FernNPRCore.StableDiffusionGraph.SDGraph.Core;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FernNPRCore.StableDiffusionGraph.SDGraph
{
    [CustomEditor(typeof(InpaintHelper))]
    public class InpaintHelperEditor : Editor
    {
        bool isRender = false;

        private SerializedProperty m_renderersProp;
        private static readonly int IsSDInPaint = Shader.PropertyToID("_Is_SDInPaint");

        private void OnEnable()
        {
            m_renderersProp = serializedObject.FindProperty("m_renderers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            InpaintHelper inpaint = (InpaintHelper)target;
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Material Inpaint", EditorStyles.boldLabel, GUILayout.Width(120));
            foreach (Material mat in inpaint.m_Materials)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(mat.name);
                inpaint.materialCheckDict[mat] = EditorGUILayout.ToggleLeft("Inpaint", inpaint.materialCheckDict[mat], GUILayout.Width(60)); 
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Set All To Inpaint"))
            {
                foreach (var key in inpaint.materialCheckDict.Keys.ToList())
                {
                    key.SetFloat(IsSDInPaint, 1);
                    inpaint.materialCheckDict[key] = true;
                }
            }
            
            if (GUILayout.Button("Set Inpaint To Material"))
            {
                foreach (var mcDict in inpaint.materialCheckDict)
                {
                    mcDict.Key.SetFloat(IsSDInPaint, mcDict.Value ? 1 : 0);
                }
            }
            
            if (GUILayout.Button("Clear Inpaint To Material"))
            {
                foreach (var key in inpaint.materialCheckDict.Keys.ToList())
                {
                    key.SetFloat(IsSDInPaint, 0);
                    inpaint.materialCheckDict[key] = false;
                }
            }
            
            if (GUILayout.Button("Enable Inpaint Clear Shading"))
            {
                foreach (var key in inpaint.materialCheckDict.Keys.ToList())
                {
                    key.SetFloat("_ClearShading", inpaint.materialCheckDict[key] ? 1 : 0);
                }
            }
            
            if (GUILayout.Button("Disable Inpaint Clear Shading"))
            {
                foreach (var key in inpaint.materialCheckDict.Keys.ToList())
                {
                    key.SetFloat("_ClearShading", 0);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
