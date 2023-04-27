using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PreprocessLineData", menuName = "Preprocess Line/Prefab Data")]
public class PreprocessLineData : ScriptableObject {
    public Mesh mesh;
    public List<DegradedQuad> degradedQuads;
    public List<Color> colors;

    [ContextMenu("Generate Prefab Data")]
    public void Generate() {
        if (mesh == null) {
            Debug.LogError("mesh is null");
            return;
        }

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        int trianglesCount = triangles.Length / 3;

        // 检测并收集所有边, 不包括掉重复的边
        var degradedQuadsDic = new Dictionary<string, DegradedQuad>();
        for (int i = 0; i < trianglesCount; i++) {
            int vertex1Index = triangles[i * 3];
            int vertex2Index = triangles[i * 3 + 1];
            int vertex3Index = triangles[i * 3 + 2];

            AddCustomLine(vertex1Index, vertex2Index, vertex3Index, vertices, degradedQuadsDic);
            AddCustomLine(vertex2Index, vertex3Index, vertex1Index, vertices, degradedQuadsDic);
            AddCustomLine(vertex3Index, vertex1Index, vertex2Index, vertices, degradedQuadsDic);
        }

        degradedQuads = new List<DegradedQuad>(degradedQuadsDic.Count);
        foreach (var degradedQuad in degradedQuadsDic) {
            degradedQuads.Add(degradedQuad.Value);
        }

        colors = new List<Color>();
        for (int i = 0; i < vertices.Length; i++) {
            colors.Add(new Color());
        }

        Debug.Log("Successfully Generate Preprocess Line Data");
    }

    private string GetLineId(Vector3 point1, Vector3 point2) {
        return string.Format("({0:f4},{1:f4},{2:f4})-({3:f4},{4:f4},{5:f4})", point1.x, point1.y, point1.z, point2.x, point2.y, point2.z);
    }

    private void AddCustomLine(int vertex1Index, int vertex2Index, int vertex3Index, Vector3[] meshVertices, Dictionary<string, DegradedQuad> degradedQuadMDic) {
        Vector3 point1 = meshVertices[vertex1Index];
        Vector3 point2 = meshVertices[vertex2Index];
        DegradedQuad degradedQuad;
        if (degradedQuadMDic.TryGetValue(GetLineId(point1, point2), out degradedQuad)) {
            if (degradedQuad.triangle2Vertex3 == -1) {
                degradedQuad.triangle2Vertex3 = vertex3Index;
                degradedQuadMDic[GetLineId(point1, point2)] = degradedQuad;
            }
        } else if (degradedQuadMDic.TryGetValue(GetLineId(point2, point1), out degradedQuad)) {
            if (degradedQuad.triangle2Vertex3 == -1) {
                degradedQuad.triangle2Vertex3 = vertex3Index;
                degradedQuadMDic[GetLineId(point2, point1)] = degradedQuad;
            }
        } else {
            degradedQuad = new DegradedQuad();
            degradedQuad.vertex1 = vertex1Index;
            degradedQuad.vertex2 = vertex2Index;
            degradedQuad.triangle1Vertex3 = vertex3Index;
            degradedQuad.triangle2Vertex3 = -1;
            degradedQuadMDic.Add(GetLineId(point1, point2), degradedQuad);
        }
    }
}

//退化四边形
[System.Serializable]
public struct DegradedQuad {
    public int vertex1;// 构成边的顶点1的索引
    public int vertex2;// 构成边的顶点2的索引
    public int triangle1Vertex3;// 边所在三角面1的顶点3索引
    public int triangle2Vertex3;// 边所在三角面2的顶点3索引
}