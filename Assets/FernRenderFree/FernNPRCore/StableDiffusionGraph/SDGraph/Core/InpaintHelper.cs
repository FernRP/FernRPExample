using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace FernNPRCore.StableDiffusionGraph
{
    [ExecuteAlways]
    public class InpaintHelper : MonoBehaviour
    {
        public Renderer[] m_renderers;
        public List<Material> m_Materials = new List<Material>();
        public Dictionary<Material, bool> materialCheckDict = new Dictionary<Material, bool>(); 

        private void OnEnable()
        {
            m_renderers = GetComponentsInChildren<Renderer>();
            m_Materials.Clear();
            materialCheckDict.Clear();
            foreach (var mRender in m_renderers)
            {
                var mat = mRender.sharedMaterials.ToArray();
                m_Materials.AddRange(mat);
            }

            foreach (var mat in m_Materials)
            {
                materialCheckDict.Add(mat, false);
            }
        }
    }
}
