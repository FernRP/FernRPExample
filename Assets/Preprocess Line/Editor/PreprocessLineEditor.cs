using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PreprocessLine {
    public class PreprocessLineEditor : EditorWindow {
        private static PreprocessLineEditor window = null;
        private bool readyPaint = false;
        private bool painting = false;
        private bool targetRootActiveSelf = true;
        private GameObject target = null;
        private GameObject debug = null;
        private Transform targetRoot = null;
        private Vector3[] debugVertices = null;
        private Mesh targetMeshColliderMesh = null;
        private MeshCollider targetMeshCollider = null;
        private MeshCollider debugMeshCollider = null;
        private PreprocessLineCore targetPPLC = null;
        private PreprocessLineCore debugPPLC = null;
        private Color brushColor = Color.red;
        private float brushMinRadius = 0;
        private float brushMaxRadius = 1;
        private float brushRadius = 0.05f;
        private int brushType = 0;
        private int paintType = 0;
        private string[] brushTypes = new string[] { "Boundary", "Outline", "Crease", "Force" };
        private LineType[] debugTypes = new LineType[] { LineType.Boundary, LineType.Outline, LineType.Crease, LineType.Force };
        private string[] paintTypes = new string[] { "Eraser", "Brush" };
        private Color[] brushColors = new Color[] {
            new Color(0.5f, 0, 0), 
            new Color(0, 1, 1), 
            new Color(0.33203125f, 0.41796875f, 0.18359375f), 
            new Color(147 / 255.0f, 112 / 255.0f, 219 / 255.0f), 
            Color.red, 
            Color.green, 
            Color.blue, 
            Color.magenta
        };
        private List<Color> oldColors = null;

        [MenuItem("Tools/Preprocess Line/Batch")]
        public static void Batch() {
            var gameObject = Selection.activeGameObject;
            if (gameObject == null) { return; }

            var meshFilterList = gameObject.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilterList.Length; i++) {
                var meshFilter = meshFilterList[i];
                var mesh = meshFilter.sharedMesh;
                if (mesh != null) {
                    var preprocessLine = meshFilter.gameObject.GetComponent<PreprocessLineForMeshFilter>();
                    if (preprocessLine == null) {
                        preprocessLine = meshFilter.gameObject.AddComponent<PreprocessLineForMeshFilter>();
                    }
                    preprocessLine.preData = BatchSave(mesh);
                    preprocessLine.enabled = true;
                }
            }

            var skinnedMeshFilterList = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedMeshFilterList.Length; i++) {
                var skinnedMeshFilter = skinnedMeshFilterList[i];
                var mesh = skinnedMeshFilter.sharedMesh;
                if (mesh != null) {
                    var preprocessLine = skinnedMeshFilter.gameObject.GetComponent<PreprocessLineForSkinnedMesh>();
                    if (preprocessLine == null) {
                        preprocessLine = skinnedMeshFilter.gameObject.AddComponent<PreprocessLineForSkinnedMesh>();
                    }
                    preprocessLine.preData = BatchSave(mesh);
                    preprocessLine.enabled = true;
                }
            }
        }

        private static PreprocessLineData BatchSave(Mesh mesh) {
            var data = CreateInstance<PreprocessLineData>();
            data.mesh = mesh;
            data.name = mesh.name;
            data.Generate();
            AssetDatabase.CreateAsset(data, "Assets/" + data.name + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return data;
        }

        [MenuItem("Tools/Preprocess Line/Editor")]
        private static void CreateWindow() {
            int x = 150, y = 150, w = 400, h = 150;
            window = (PreprocessLineEditor)GetWindow(typeof(PreprocessLineEditor), false);
            window.position = new Rect(x, y, w + x, h + y);
            window.minSize = new Vector2(w, h);
            window.maxSize = new Vector2(w + 1, h + 1);
            window.titleContent = new GUIContent("Preprocess Line Editor");
            window.Show();
        }

        private void OnEnable() {
            if (window == null) {
                CreateWindow();
            }
#if UNITY_2018
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#elif UNITY_2019
            SceneView.duringSceneGui += OnSceneGUI;
#endif
            OnSelectionChange();
        }

        private void OnDisable() {
            if (window != null) {
                window = null;
            }
#if UNITY_2018
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#elif UNITY_2019
            SceneView.duringSceneGui -= OnSceneGUI;
#endif
            reset();
        }

        private void OnSelectionChange() {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null) { return; }

            if (selectedObject == debug) {
                startEditor();
                return;
            }

            reset();
            target = selectedObject;
            targetPPLC = target.GetComponent<PreprocessLineCore>();
            if (targetPPLC == null) { return; }

            if (targetPPLC.preData.mesh == null) { return; }

            targetMeshCollider = target.GetComponent<MeshCollider>();
            if (targetMeshCollider != null) {
                targetMeshColliderMesh = targetMeshCollider.sharedMesh;
            }

            readyPaint = true;
            window.Repaint();
        }

        private void OnProjectChange() {
            window.Repaint();
        }

        private void OnInspectorUpdate() {
            this.Repaint();
        }

        private void OnGUI() {
            if (!readyPaint) {
                string warning = "未知警告";
                if (target == null) {
                    warning = "Please to select a game object";
                } else if (targetPPLC == null) {
                    warning = "Object which is selected must have \"PreprocessLineForMeshFilter\" or \"PreprocessLineForSkinnedMesh\" component";
                } else if (targetPPLC.preData.mesh == null) {
                    warning = "Mesh is not found, don't remove the mesh of PreprocessLineData";
                }
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
                return;
            }

            if (!painting) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start to edit", GUILayout.Height(30))) {
                    debug = Instantiate(target);
                    debug.name = "Preprocess line editor (auto destroy)";
                    Vector3 worldPosition = target.transform.position;
                    Quaternion worldRotation = target.transform.rotation;
                    debug.transform.parent = null;
                    debug.transform.position = worldPosition;
                    debug.transform.rotation = worldRotation;
                    debugPPLC = debug.GetComponent<PreprocessLineCore>();
                    Selection.activeObject = debug;
                }
                EditorGUILayout.EndHorizontal();
            } else {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Line Type: ", GUILayout.Width(90));
                brushType = GUILayout.Toolbar(brushType, brushTypes);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Brush Type: ", GUILayout.Width(90));
                paintType = GUILayout.Toolbar(paintType, paintTypes);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Brush Min Radius: ");
                brushMinRadius = EditorGUILayout.FloatField(brushMinRadius);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Brush Max Radius: ");
                brushMaxRadius = EditorGUILayout.FloatField(brushMaxRadius);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Brush Radius: ", GUILayout.Width(90));
                brushRadius = GUILayout.HorizontalSlider(brushRadius, brushMinRadius, brushMaxRadius, GUILayout.Height(20));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Brush Radius: " + brushRadius);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear All State", GUILayout.Height(20), GUILayout.Width(100))) {
                    if (EditorUtility.DisplayDialog("Warning", "Did you want to clear all state which you have set, no matter what now or past?", "yes", "no")) {
                        var data = targetPPLC.preData;
                        for (int i = 0; i < data.colors.Count; i++) {
                            data.colors[i] = new Color(0, 0, 0, 0);
                        }
                    }
                }
                if (GUILayout.Button("Recovery All State", GUILayout.Height(20))) {
                    if (EditorUtility.DisplayDialog("Warning", "Did you want to give up all state which you set this time?", "yes", "no")) {
                        var data = targetPPLC.preData;
                        for (int i = 0; i < data.colors.Count; i++) {
                            data.colors[i] = oldColors[i];
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                brushColor = brushColors[brushType + paintType * brushTypes.Length];
                debugPPLC.SetDebugLineType(debugTypes[brushType]);
            }

        }

        private void OnSceneGUI(SceneView sceneView) {
            if (!painting) { return; }

            Event current = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            RaycastHit hit;
            int controlID = GUIUtility.GetControlID(sceneView.GetHashCode(), FocusType.Passive);
            switch (current.GetTypeForControl(controlID)) {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlID);
                    break;
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (current.GetTypeForControl(controlID) == EventType.MouseDrag && GUIUtility.hotControl != controlID) {
                        return;
                    }

                    if (current.alt || current.control || current.button != 0 || HandleUtility.nearestControl != controlID) {
                        return;
                    }

                    if (current.type == EventType.MouseDown) {
                        GUIUtility.hotControl = controlID;
                    }

                    if (Physics.Raycast(ray, out hit, float.MaxValue) && hit.transform == debug.transform) {
                        Vector3 hitPos = Vector3.Scale(debug.transform.InverseTransformPoint(hit.point), debug.transform.localScale);
                        for (int i = 0; i < debugVertices.Length; i++) {
                            Vector3 vertPos = Vector3.Scale(debugVertices[i], debug.transform.localScale);
                            if ((vertPos - hitPos).sqrMagnitude > brushRadius * brushRadius)
                                continue;

                            Color temp = targetPPLC.preData.colors[i];
                            if (paintType == 0 && brushType == 0) {
                                temp.r = 1;
                            } else if (paintType == 0 && brushType == 1) {
                                temp.g = 1;
                            } else if (paintType == 0 && brushType == 2) {
                                temp.b = 1;
                            } else if (paintType == 0 && brushType == 3) {
                                temp.a = 0;
                            } else if (paintType == 1 && brushType == 0) {
                                temp.r = 0;
                            } else if (paintType == 1 && brushType == 1) {
                                temp.g = 0;
                            } else if (paintType == 1 && brushType == 2) {
                                temp.b = 0;
                            } else if (paintType == 1 && brushType == 3) {
                                temp.a = 1;
                            }
                            targetPPLC.preData.colors[i] = temp;
                        }
                    }
                    current.Use();
                    break;
                case EventType.MouseUp:
                    break;
                case EventType.Repaint:
                    if (Physics.Raycast(ray, out hit, float.MaxValue)) {
                        if (hit.transform == debug.transform) {
                            Handles.color = brushColor;
                            Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);
                        }
                    }
                    HandleUtility.Repaint();
                    break;
            }
        }

        private void startEditor() {
            if (targetMeshCollider == null) {
                debugMeshCollider = debug.AddComponent<MeshCollider>();
                debugMeshCollider.sharedMesh = targetPPLC.preData.mesh;
            } else {
                targetMeshCollider.sharedMesh = targetPPLC.preData.mesh;
            }

            targetRoot = target.transform;
            while (targetRoot.parent != null) {
                targetRoot = targetRoot.parent;
            }

            oldColors = new List<Color>(targetPPLC.preData.colors);
            targetRootActiveSelf = targetRoot.gameObject.activeSelf;
            targetRoot.gameObject.SetActive(false);
            debugVertices = targetPPLC.preData.mesh.vertices;
            painting = true;
            Tools.hidden = true;
        }

        private void reset() {
            painting = false;
            readyPaint = false;
            Tools.hidden = false;
            brushColor = Color.red;
            if (targetMeshCollider != null) {
                targetMeshCollider.sharedMesh = targetMeshColliderMesh;
            } else if (debugMeshCollider != null) {
                DestroyImmediate(debugMeshCollider);
            }

            if (debug != null) {
                DestroyImmediate(debug);
                debug = null;
            }

            if (targetRoot != null) {
                targetRoot.gameObject.SetActive(targetRootActiveSelf);
                targetRoot = null;
            }

            if (debugPPLC != null) {
                debugPPLC.SetDebugLineType(LineType.None);
            }

            if (targetPPLC != null) {
                EditorUtility.SetDirty(targetPPLC.preData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }

}
