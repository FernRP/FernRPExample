using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PreprocessLine {
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class PreprocessLineForMeshFilter : PreprocessLineCore {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void OnEnable() {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            render = meshRenderer;
            Initialize();
        }

        private void OnDisable() {
            Release();
        }

        public override Mesh GetMesh() {
            return meshFilter.sharedMesh;
        }

        public override void Draw(CommandBuffer command) {
            DrawForMeshFilter(command);
        }
    }
}
