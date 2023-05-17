using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using AquaSys.SmoothNormals;

namespace AquaSys.TextureBaker.Editor
{
    public class AquaTextureBakerWindow : EditorWindow
    {
        private static AquaTextureBakerWindow instance;

        public static AquaTextureBakerWindow Instance
        {
            get
            {
                return instance;
            }
        }

        [System.Serializable]
        class DataWarpper
        {
            public List<TextureBlock> TextureBlocks = new List<TextureBlock>();
            public List<string> modelPaths = new List<string>();
        }

        public Dictionary<TargetVertexAttribute, List<TextureBlock>> TextureBlockPipline = new Dictionary<TargetVertexAttribute, List<TextureBlock>>();

        class TableModelData
        {
            public string path;
            public GameObject model;
            public int meshCount;
            public int submeshCount;
            public int vertexCount;
            public int triangleCount;
        }

        private static string DATA_PATH_KEY ;

        DataWarpper dataWarpper;
        List<TableModelData> tableModelDatas = new List<TableModelData>();

        private bool ShowConfigs = true;

        private Vector2 scrollPosition;
        bool dataChanged = false;

        static readonly string FlagLabel = "AquaTextureBaker";
        public static bool toAddLabel;
        public static bool toClearLabel;
        public static bool CheckLabel(AssetImporter assetImporter)
        {
            foreach (var label in AssetDatabase.GetLabels(assetImporter))
            {
                if (label.Contains(FlagLabel))
                {
                    return true;
                }
            }
            return false;
        }

        public static void AddLabel(AssetImporter assetImporter)
        {
            var labels = new List<string>(AssetDatabase.GetLabels(assetImporter));
            if (!labels.Contains(FlagLabel))
            {
                labels.Add(FlagLabel);
            }
            else
            {
                Debug.LogWarning("Lable has beed added!");
            }

            AssetDatabase.SetLabels(assetImporter, labels.ToArray());
        }

        public static void RemoveLabel(AssetImporter assetImporter)
        {
            var labels = new List<string>(AssetDatabase.GetLabels(assetImporter));
            if (labels.Contains(FlagLabel))
            {
                labels.Remove(FlagLabel);
            }

            AssetDatabase.SetLabels(assetImporter, labels.ToArray());
        }

        [MenuItem("Tools/AquaTools/Aqua Texture Baker")]
        static void Init()
        {
            instance = GetWindow<AquaTextureBakerWindow>("AquaTextureBakerWindow");
            instance.Show();
        }

        private void OnDisable()
        {
            instance = null;
        }

        private void OnEnable()
        {
            instance = this;
            DATA_PATH_KEY = $"{Application.productName}_AquaTextureBakerWindow.DataPath";
            if (EditorPrefs.HasKey(DATA_PATH_KEY))
            {
                string path = EditorPrefs.GetString(DATA_PATH_KEY);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    dataWarpper = JsonConvert.DeserializeObject<DataWarpper>(json);
                    dataChanged = false;
                    return;
                }
            }
            dataWarpper = new DataWarpper();
        }

        void OnGUI()
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();
            ShowConfigs = EditorGUILayout.Foldout(ShowConfigs, "Configs");
            GUILayout.FlexibleSpace();
            var originalBackgroundColor = GUI.backgroundColor;

       
            if(GUILayout.Button("Save As Template"))
            {
                string path = EditorUtility.SaveFilePanel("Save As Template", "", "Template.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    var template = new DataWarpper();
                    template.TextureBlocks = dataWarpper.TextureBlocks;
                    string json = JsonConvert.SerializeObject(template, Formatting.Indented);
                    File.WriteAllText(path, json);
                }
            }
            if (GUILayout.Button("Load Template"))
            {
                string path = EditorUtility.OpenFilePanel("Load Template", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    var template = JsonConvert.DeserializeObject<DataWarpper>(json);
                    dataWarpper.TextureBlocks = template.TextureBlocks;
                }
            }
            if (dataChanged)
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = originalBackgroundColor;
            if (GUILayout.Button("SaveData"))
            {
                string path = EditorUtility.SaveFilePanel("Save Data", "", "data.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = JsonConvert.SerializeObject(dataWarpper, Formatting.Indented);
                    File.WriteAllText(path, json);
                    EditorPrefs.SetString(DATA_PATH_KEY, path);
                    dataChanged = false;
                }
            }
            GUI.backgroundColor = originalBackgroundColor;
            if (GUILayout.Button("LoadData"))
            {
                string path = EditorUtility.OpenFilePanel("Load Data", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    string json = File.ReadAllText(path);
                    dataWarpper = JsonConvert.DeserializeObject<DataWarpper>(json);
                    dataChanged = false;
                }
            }
            if (GUILayout.Button("Create New"))
            {
                dataWarpper = new DataWarpper();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            if (ShowConfigs)
            {
                EditorGUI.indentLevel++;
                if (dataWarpper.TextureBlocks.Count == 0)
                {
                    dataWarpper.TextureBlocks.Add(new TextureBlock() { TargetColorChannel = TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A });
                    dataChanged = true;
                }
                for (int i = 0; i < dataWarpper.TextureBlocks.Count; i++)
                {
                    TextureBlock block = dataWarpper.TextureBlocks[i];
                    EditorGUILayout.BeginHorizontal();
                    block.Foldout = EditorGUILayout.Foldout(block.Foldout, block.Name);
                    GUILayout.FlexibleSpace();
                    if (dataWarpper.TextureBlocks.Count > 1)
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            dataChanged = true;
                            dataWarpper.TextureBlocks.RemoveAt(i);
                            i--;
                            EditorGUILayout.EndVertical();
                            continue;
                        }
                    EditorGUILayout.EndHorizontal();
                    if (block.Foldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.BeginHorizontal();
                        string newName = EditorGUILayout.TextField("Name", block.Name);
                        if (newName != block.Name)
                        {
                            dataChanged = true;
                            block.Name = newName;
                         
                        }
                        var newDataSource = (DataSource)EditorGUILayout.EnumPopup("DataSource", block.DataSource);
                        if(newDataSource != block.DataSource)
                        {
                            dataChanged = true;
                            block.DataSource = newDataSource;
                          
                        }
                        var prevSourceChannel = block.SourceColorChannel;

                        switch (block.DataSource)
                        {
                            case DataSource.MaterialTexture:
                                if (tableModelDatas.Count > 0 && block.TextureNameOptions.Count == 0)
                                {
                                    block.UpdateTextureNameOptions(tableModelDatas[0].model);
                                }
                                int textureNameIndex = EditorGUILayout.Popup("TextureName", block.TextureNameOptions.IndexOf(block.TextureName), block.TextureNameOptions.ToArray());
                                if (textureNameIndex >= 0)
                                {
                                    var newTextureName= block.TextureNameOptions[textureNameIndex];
                                    if(newTextureName != block.TextureName)
                                    {
                                        block.TextureName = newTextureName;
                                        dataChanged = true;
                                    }
                                }
                                if (GUILayout.Button("R", GUILayout.Width(20)))
                                {
                                    if (tableModelDatas.Count > 0)
                                        block.UpdateTextureNameOptions(tableModelDatas[0].model);
                                }
                                break;
                            case DataSource.CustomTexture:
                                if (block.CustomTexture == null && !string.IsNullOrEmpty(block.CustomTexturePath))
                                {
                                    if (File.Exists(block.CustomTexturePath))
                                    {
                                        block.CustomTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(block.CustomTexturePath);
                                    }
                                    else
                                    {
                                        block.CustomTexturePath = "";
                                    }
                                }
                                var newCustomTexture = (Texture2D)EditorGUILayout.ObjectField("CustomTexture", block.CustomTexture, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                                if(newCustomTexture != block.CustomTexture)
                                {
                                    block.CustomTexture = newCustomTexture;
                                    block.CustomTexturePath = AssetDatabase.GetAssetPath(block.CustomTexture);
                                    dataChanged = true;
                                }
                                break;
                            case DataSource.SmoothedNormal:
                                block.SmoothedNormalType = (SmoothedNormalType)EditorGUILayout.EnumPopup("NormalType", block.SmoothedNormalType);
                                switch (block.SmoothedNormalType)
                                {
                                    case SmoothedNormalType.FullVector3:
                                        block.SourceColorChannel = SourceColorChannel.RGB;
                                        break;
                                    case SmoothedNormalType.CompressedVector2:
                                        block.SourceColorChannel = SourceColorChannel.RG;
                                        break;
                                }
                                break;
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(block.DataSource == DataSource.SmoothedNormal);
                        block.SourceColorChannel = (SourceColorChannel)EditorGUILayout.EnumPopup("Source Color Channel", block.SourceColorChannel);
                        EditorGUI.EndDisabledGroup();
                        if (prevSourceChannel != block.SourceColorChannel)
                        {
                            switch (block.SourceColorChannel)
                            {
                                case SourceColorChannel.RGBA:
                                    block.TargetColorChannel = TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A;
                                    break;
                                case SourceColorChannel.RGB:
                                    block.TargetColorChannel = TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B;
                                    break;
                                case SourceColorChannel.RG:
                                    block.TargetColorChannel = TargetColorChannel.R | TargetColorChannel.G;
                                    break;
                                case SourceColorChannel.Red:
                                    block.TargetColorChannel = TargetColorChannel.R;
                                    break;
                                case SourceColorChannel.Green:
                                    block.TargetColorChannel = TargetColorChannel.G;
                                    break;
                                case SourceColorChannel.Blue:
                                    block.TargetColorChannel = TargetColorChannel.B;
                                    break;
                                case SourceColorChannel.Alpha:
                                    block.TargetColorChannel = TargetColorChannel.A;
                                    break;
                                case SourceColorChannel.Grayscale:
                                    block.TargetColorChannel = TargetColorChannel.R;
                                    break;
                            }
                        }
                        var newTargetMeshDataChannel = (TargetVertexAttribute)EditorGUILayout.EnumPopup("TargetVertexAttribute", block.TargetMeshDataChannel);
                        if (newTargetMeshDataChannel != block.TargetMeshDataChannel)
                        {
                            block.TargetMeshDataChannel=newTargetMeshDataChannel;
                            dataChanged = true;
                        }
                        originalBackgroundColor = GUI.backgroundColor;
                        EditorGUI.BeginDisabledGroup(block.SourceColorChannel == SourceColorChannel.RGBA || block.SourceColorChannel == SourceColorChannel.RGB || block.SourceColorChannel == SourceColorChannel.RG);
                        GUI.backgroundColor = (block.TargetColorChannel & TargetColorChannel.R) != 0 ? Color.green : Color.gray;
                        if (GUILayout.Button("R"))
                            block.TargetColorChannel = TargetColorChannel.R;
                        GUI.backgroundColor = (block.TargetColorChannel & TargetColorChannel.G) != 0 ? Color.green : Color.gray;
                        if (GUILayout.Button("G"))
                            block.TargetColorChannel = TargetColorChannel.G;
                        GUI.backgroundColor = (block.TargetColorChannel & TargetColorChannel.B) != 0 ? Color.green : Color.gray;
                        if (GUILayout.Button("B"))
                            block.TargetColorChannel = TargetColorChannel.B;
                        GUI.backgroundColor = (block.TargetColorChannel & TargetColorChannel.A) != 0 ? Color.green : Color.gray;
                        if (GUILayout.Button("A"))
                            block.TargetColorChannel = TargetColorChannel.A;
                        EditorGUI.EndDisabledGroup();
                        GUI.backgroundColor = originalBackgroundColor;

                        var newDataWriteType = (ChannelDataWriteType)EditorGUILayout.EnumPopup("ChannelWriteType", block.DataWriteType);
                        if(newDataWriteType != block.DataWriteType)
                        {
                            block.DataWriteType = newDataWriteType;
                            dataChanged = true;
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                        EditorGUI.indentLevel--;
                    }
                       
                }

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    dataWarpper.TextureBlocks.Add(new TextureBlock() { TargetColorChannel = TargetColorChannel.R | TargetColorChannel.G | TargetColorChannel.B | TargetColorChannel.A });
                    dataChanged = true;
                }

                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Drag and drop models here:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Model List"))
            {
                dataWarpper.modelPaths.Clear();
                tableModelDatas.Clear();
                dataChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "");

            if (Event.current.type == EventType.DragUpdated && dropArea.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && dropArea.Contains(Event.current.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                dataChanged = true;
                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    var path = AssetDatabase.GetAssetPath(draggedObject);
                    if (Directory.Exists(path))
                    {
                        var files = Directory.GetFiles(path, "*.fbx", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            if (!dataWarpper.modelPaths.Contains(file))
                                dataWarpper.modelPaths.Add(file);                           
                        }
                    }
                    else
                    {
                        if (PrefabUtility.IsPartOfPrefabAsset(draggedObject))
                        {
                            path = AssetDatabase.GetAssetPath(draggedObject);
                        }
                        else if (PrefabUtility.IsPartOfPrefabInstance(draggedObject))
                        {
                            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(draggedObject);
                            path = AssetDatabase.GetAssetPath(prefabAsset);
                        }
                        if(!dataWarpper.modelPaths.Contains(path))
                            dataWarpper.modelPaths.Add(path);
                    }   
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("MeshCount", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("SubMeshCount", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("VertexCount&TriangleCount", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            if (tableModelDatas.Count < dataWarpper.modelPaths.Count)
            {
                for (int i = 0; i < dataWarpper.modelPaths.Count; i++)
                {
                    if(tableModelDatas.Find(_=>_.path== dataWarpper.modelPaths[i]) == null)
                    {
                        var data = new TableModelData();
                        data.path = dataWarpper.modelPaths[i];
                        if (!File.Exists(data.path))
                            continue;
                        data.model = AssetDatabase.LoadAssetAtPath<GameObject>(dataWarpper.modelPaths[i]);
                        var meshes = Utils.GetMesh(data.model);
                        data.meshCount = meshes.Count;
                        if (meshes.Count > 0)
                        {
                            int submeshCount = 0;
                            foreach (var item in meshes)
                            {
                                submeshCount += item.Key.subMeshCount;
                            }
                            data.submeshCount = submeshCount;
                            int vertexCount = 0;
                            foreach (var item in meshes)
                            {
                                vertexCount += item.Key.vertexCount;
                            }
                            data.vertexCount= vertexCount;
                            int triangleCount = 0;
                            foreach (var item in meshes)
                            {
                                triangleCount += item.Key.triangles.Length;
                            }
                            data.triangleCount= triangleCount/3;
                        }
                        tableModelDatas.Add(data);
                    }
                }
               
            }
            if (tableModelDatas.Count > dataWarpper.modelPaths.Count)
            {
                for (int i = tableModelDatas.Count - 1; i>=0; i--)
                {
                    if (!dataWarpper.modelPaths.Contains(tableModelDatas[i].path))
                    {
                        tableModelDatas.RemoveAt(i);
                    }
                }
            }
            
            for (int i = 0; i < tableModelDatas.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(tableModelDatas[i].model, typeof(GameObject), false);
                EditorGUILayout.LabelField(tableModelDatas[i].meshCount.ToString());
                EditorGUILayout.LabelField(tableModelDatas[i].submeshCount.ToString());
                EditorGUILayout.LabelField($"{tableModelDatas[i].vertexCount.ToString("N0")} / {tableModelDatas[i].triangleCount.ToString("N0")}");

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    dataChanged = true;
                    tableModelDatas.RemoveAt(i);
                    dataWarpper.modelPaths.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Space(15);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Bake Texture To Mesh", GUILayout.Height(40)))
            {
                if (tableModelDatas.Count == 0)
                {
                    Debug.LogError("Please select a model first.");
                    return;
                }
                TextureBlockPipline = new Dictionary<TargetVertexAttribute, List<TextureBlock>>();

                for (int i = 0; i < dataWarpper.TextureBlocks.Count; i++)
                {
                    if (!TextureBlockPipline.ContainsKey(dataWarpper.TextureBlocks[i].TargetMeshDataChannel))
                    {
                        TextureBlockPipline[dataWarpper.TextureBlocks[i].TargetMeshDataChannel] = new List<TextureBlock>();
                    }
                    TextureBlockPipline[dataWarpper.TextureBlocks[i].TargetMeshDataChannel].Add(dataWarpper.TextureBlocks[i]);

                }

                toAddLabel = true;

                foreach (var data in tableModelDatas)
                {
                    AssetDatabase.ImportAsset(data.path);
                }

                toAddLabel = false;
            }
        }
    }

    public class Utils
    {
        public static Dictionary<Mesh, Material> GetMesh(GameObject go)
        {
            Dictionary<Mesh, Material> results = new Dictionary<Mesh, Material>();
            var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var item in skinnedMeshRenderers)
                results[item.sharedMesh] = item.sharedMaterial;

            var meshFilters = go.GetComponentsInChildren<MeshFilter>();
            foreach (var item in meshFilters)
            {
                var meshRenderer = item.GetComponent<MeshRenderer>();
                results[item.sharedMesh] = meshRenderer.sharedMaterial;
            }

            return results;
        }
    }

    [System.Serializable]
    public class TextureBlock
    {
        public string Name;
        public bool Foldout = true;
        [JsonConverter(typeof(StringEnumConverter))]
        public DataSource DataSource;
        public string CustomTexturePath;
        [System.NonSerialized]
        public Texture2D CustomTexture;
        public string TextureName;
        [JsonConverter(typeof(StringEnumConverter))]
        public SmoothedNormalType SmoothedNormalType;
        [System.NonSerialized]
        public List<string> TextureNameOptions = new List<string>();
        [JsonConverter(typeof(StringEnumConverter))]
        public TargetVertexAttribute TargetMeshDataChannel;
        [JsonConverter(typeof(StringEnumConverter))]
        public SourceColorChannel SourceColorChannel;
        [JsonConverter(typeof(StringEnumConverter))]
        public TargetColorChannel TargetColorChannel;
        [JsonConverter(typeof(StringEnumConverter))]
        public ChannelDataWriteType DataWriteType;
        public void UpdateTextureNameOptions(GameObject model)
        {
            TextureNameOptions.Clear();
            if (model != null)
            {
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material material = renderer.sharedMaterial;
                    if (material != null)
                    {
                        Shader shader = material.shader;
                        int propertyCount = ShaderUtil.GetPropertyCount(shader);
                        for (int i = 0; i < propertyCount; i++)
                        {
                            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            {
                                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                                if (!TextureNameOptions.Contains(propertyName))
                                {
                                    TextureNameOptions.Add(propertyName);
                                }
                            }
                        }
                    }
                }
            }
        }
    } 
}