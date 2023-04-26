using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PreprocessLine {
    [ExecuteInEditMode]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class PreprocessLineForSkinnedMesh : PreprocessLineCore {
        private SkinnedMeshRenderer meshRenderer;
        private Mesh mesh;

        private void OnEnable() {
            mesh = new Mesh();
            meshRenderer = GetComponent<SkinnedMeshRenderer>();
            render = meshRenderer;
            Initialize();
        }

        private void OnDisable() {
            Release();
            DestroyImmediate(mesh);
        }

        public override Mesh GetMesh() {
            meshRenderer.BakeMesh(mesh);
            mesh.RecalculateNormals();
            return mesh;
        }

        public override void Draw(CommandBuffer command) {
            DrawForSkinnedMesh(command);
        }
    }
}
