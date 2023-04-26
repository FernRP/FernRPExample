using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PreprocessLine {
    public abstract class PreprocessLineCore : MonoBehaviour {
        public PreprocessLineData preData;
        [Range(0, 0.02f)]
        public float lineWidth = 0.005f;
        public Color lineColor = Color.black;
        public bool scale = true;

        private Vector4 debugLineType = Vector4.one;
        private static List<PreprocessLineCore> lines = new List<PreprocessLineCore>();
        private Material drawMaterial;
        private ComputeBuffer verticesBuffer;
        private ComputeBuffer normalsBuffer;
        private ComputeBuffer colorsBuffer;
        private ComputeBuffer degradedQuadsBuffer;
        private List<Vector3> vertices;
        private List<Vector3> normals;
        private List<Color> colors;
        private List<DegradedQuad> quads;
        private bool isVisible = false;
        protected Renderer render;
        
        public PreprocessLineData PreData {
            get {
                return preData;
            }

            set {
                preData = value;
            }
        }

        public bool IsVisible() {
            return isVisible;
        }

        public static IEnumerable<PreprocessLineCore> GetCollection() {
            return lines;
        }

        public abstract Mesh GetMesh();

        public abstract void Draw(CommandBuffer command);

        protected void Initialize() {
            Shader shader = Shader.Find("Hidden/PreprocessLine-DX11");
            if (shader == null) {
                Debug.LogError("\"PreprocessLine.shader\" can not be found");
                return;
            }

            if (PreData == null) {
                enabled = false;
                Debug.LogWarning("pre-data is null, please to generate at first");
                return;
            }

            drawMaterial = new Material(shader);

            Mesh mesh = GetMesh();
            vertices = new List<Vector3>(mesh.vertices);
            normals = new List<Vector3>(mesh.normals);
            colors = PreData.colors;
            quads = PreData.degradedQuads;

            verticesBuffer = new ComputeBuffer(vertices.Count, 12);
            normalsBuffer = new ComputeBuffer(normals.Count, 12);
            colorsBuffer = new ComputeBuffer(colors.Count, 16);
            degradedQuadsBuffer = new ComputeBuffer(quads.Count, Marshal.SizeOf(typeof(DegradedQuad)));

            SetData();
            SetBuffers();
            lines.Add(this);
        }

        protected void DrawForMeshFilter(CommandBuffer command) {
#if UNITY_EDITOR
            SetData();
            SetBuffers();
#endif
            var localToWorldMatrix = render.localToWorldMatrix;
            if (scale) {
                var lossyScale = render.transform.lossyScale;
                lossyScale.x = 1.0f / lossyScale.x;
                lossyScale.y = 1.0f / lossyScale.y;
                lossyScale.z = 1.0f / lossyScale.z;
                localToWorldMatrix *= Matrix4x4.Scale(lossyScale);
            }
            command.DrawProcedural(localToWorldMatrix, drawMaterial, 0, MeshTopology.Points, degradedQuadsBuffer.count);
        }

        protected void DrawForSkinnedMesh(CommandBuffer command) {
            Mesh mesh = GetMesh();
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            verticesBuffer.SetData(vertices);
            normalsBuffer.SetData(normals);
            DrawForMeshFilter(command);
        }

        protected void Release() {
            lines.Remove(this);
            if (verticesBuffer != null) {
                verticesBuffer.Release();
            }

            if (normalsBuffer != null) {
                normalsBuffer.Release();
            }

            if (colorsBuffer != null) {
                colorsBuffer.Release();
            }

            if (degradedQuadsBuffer != null) {
                degradedQuadsBuffer.Release();
            }

            if (drawMaterial != null) {
                DestroyImmediate(drawMaterial);
            }

            verticesBuffer = null;
            normalsBuffer = null;
            colorsBuffer = null;
            degradedQuadsBuffer = null;
            drawMaterial = null;
        }

#if UNITY_EDITOR
        public void SetDebugLineType(LineType lineType) {
            switch (lineType) {
                case LineType.Boundary:
                    debugLineType.Set(1, 0, 0, 0);
                    break;
                case LineType.Outline:
                    debugLineType.Set(0, 1, 0, 0);
                    break;
                case LineType.Crease:
                    debugLineType.Set(0, 0, 1, 0);
                    break;
                case LineType.Force:
                    debugLineType.Set(0, 0, 0, 1);
                    break;
                case LineType.None:
                    debugLineType.Set(1, 1, 1, 1);
                    break;
            }
        }
#endif

        private void SetBuffers() {
            drawMaterial.SetFloat("lineWidth", lineWidth);
            drawMaterial.SetColor("lineColor", lineColor);
            drawMaterial.SetVector("debugLineType", debugLineType);

            drawMaterial.SetBuffer("vertices", verticesBuffer);
            drawMaterial.SetBuffer("normals", normalsBuffer);
            drawMaterial.SetBuffer("colors", colorsBuffer);
            drawMaterial.SetBuffer("degradedQuads", degradedQuadsBuffer);
        }

        private void SetData() {
            verticesBuffer.SetData(vertices);
            normalsBuffer.SetData(normals);
            colorsBuffer.SetData(colors);
            degradedQuadsBuffer.SetData(quads);
        }

        private void OnBecameVisible() {
            isVisible = true;
        }

        private void OnBecameInvisible() {
            isVisible = false;
        }
    }

    public enum LineType {
        Boundary,
        Outline,
        Crease,
        Force,
        None,
    }
}
