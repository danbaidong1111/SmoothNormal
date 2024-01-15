using UnityEngine;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    #region EditorWindowVer
    public class SmoothNormalTool : EditorWindow
    {
        [System.Serializable]
        public class MeshWithSmoothAsset
        {
            public MeshWithSmoothAsset(Mesh mesh)
            {
                this.mesh = mesh;
            }
            public Mesh mesh;
        }
        public enum SmoothNormalWriteTarget
        {
            VertexColor = 0,
            Tangent = 1,
        }
        public SmoothNormalWriteTarget writeTarget;

        
        public List<MeshWithSmoothAsset> m_MeshWithSmoothAssets; 

        private SerializedObject serializedObject;
        private SerializedProperty meshWithSmoothAssetsProperty;


        [MenuItem("Tools/SmoothNormalTool")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(SmoothNormalTool));
            window.position = new Rect(800, 300, 600, 800);
        }

        private void OnEnable()
        {
            if (m_MeshWithSmoothAssets == null)
                m_MeshWithSmoothAssets = new List<MeshWithSmoothAsset>();
            serializedObject = new SerializedObject(this);
            meshWithSmoothAssetsProperty = serializedObject.FindProperty("m_MeshWithSmoothAssets");
        }

        void OnGUI()
        {
            serializedObject.Update();

            GUILayout.Space(10);
            GUILayout.Label("Selected Objects:", EditorStyles.boldLabel);
            Object[] selectedObjects = Selection.objects;


            // 绘制区域接受文件拖拽
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop Mesh/fbx Objects here");

            // 处理文件拖拽事件
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        m_MeshWithSmoothAssets.Clear();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is GameObject)
                            {
                                GameObject fbxObject = (GameObject)draggedObject;

                                MeshFilter[] meshFilters = fbxObject.GetComponentsInChildren<MeshFilter>();
                                
                                foreach (MeshFilter meshFilter in meshFilters)
                                {
                                    Mesh mesh = meshFilter.sharedMesh;
                                    var meshWithSmoothAsset = new MeshWithSmoothAsset(mesh);
                                    m_MeshWithSmoothAssets.Add(meshWithSmoothAsset);
                                }

                                SkinnedMeshRenderer[] skinnedMeshs = fbxObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                                foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshs)
                                {
                                    Mesh mesh = skinnedMesh.sharedMesh;
                                    var meshWithSmoothAsset = new MeshWithSmoothAsset(mesh);
                                    m_MeshWithSmoothAssets.Add(meshWithSmoothAsset);
                                }
                            }
                            else if (draggedObject is Mesh)
                            {
                                Mesh mesh = (Mesh)draggedObject;
                                var meshWithSmoothAsset = new MeshWithSmoothAsset(mesh);
                                m_MeshWithSmoothAssets.Add(meshWithSmoothAsset);
                            }
                        }
                    }
                    break;
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(meshWithSmoothAssetsProperty);
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Smooth normal data write target:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            writeTarget = (SmoothNormalWriteTarget)EditorGUILayout.EnumPopup(writeTarget, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);



            switch (writeTarget)
            {
                case SmoothNormalWriteTarget.VertexColor:

                    break;
                case SmoothNormalWriteTarget.Tangent:

                    break;
                default:
                    Debug.LogError("No avaliable write target");
                    break;
            }
            if (GUILayout.Button("SmoothNormal"))
            {
                CalculateSmoothNormal(m_MeshWithSmoothAssets, writeTarget);
            }


            GUILayout.Space(10);
            if (GUILayout.Button("Export Mesh"))
            {
                ExportMesh();
            }

        }
        public void CalculateSmoothNormal(List<MeshWithSmoothAsset> meshAssets, SmoothNormalWriteTarget writeTarget)//Mesh选择器 修改并预览
        {
            foreach (MeshWithSmoothAsset meshAsset in meshAssets)
            {
                Mesh mesh = meshAsset.mesh;
                Vector3[] averageNormals = AverageNormal(mesh);
                WriteNormalsToMesh(mesh, averageNormals, writeTarget);

                //string assetPath = AssetDatabase.GetAssetPath(mesh);
                //AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        public Vector3[] AverageNormal(Mesh mesh)
        {
            Dictionary<Vector3, List<int>> map = new Dictionary<Vector3, List<int>>();
            for (int v = 0; v < mesh.vertexCount; ++v)
            {
                if (!map.ContainsKey(mesh.vertices[v]))
                {
                    map.Add(mesh.vertices[v], new List<int>());
                }
                map[mesh.vertices[v]].Add(v);
            }
            Vector3[] normals = mesh.normals;
            Vector3 normal;
            foreach (var p in map)
            {
                normal = Vector3.zero;
                foreach (var n in p.Value)
                {
                    normal += mesh.normals[n];
                }
                normal /= p.Value.Count;
                foreach (var n in p.Value)
                {
                    normals[n] = (normal.normalized);
                }
            }


            //构建模型空间→切线空间的转换矩阵
            List<Vector3[]> OtoTMatrixs = new List<Vector3[]>();
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                Vector3[] OtoTMatrix = new Vector3[3];
                OtoTMatrix[0] = new Vector3(mesh.tangents[i].x, mesh.tangents[i].y, mesh.tangents[i].z);
                OtoTMatrix[1] = Vector3.Cross(mesh.normals[i], OtoTMatrix[0]);
                OtoTMatrix[1] = new Vector3(OtoTMatrix[1].x * mesh.tangents[i].w, OtoTMatrix[1].y * mesh.tangents[i].w, OtoTMatrix[1].z * mesh.tangents[i].w);
                OtoTMatrix[2] = mesh.normals[i];
                OtoTMatrixs.Add(OtoTMatrix);
            }

            //将meshNormals数组中的法线值一一与矩阵相乘，求得切线空间下的法线值
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 tNormal;
                tNormal = Vector3.zero;
                tNormal.x = Vector3.Dot(((Vector3[])OtoTMatrixs[i])[0], normals[i]);
                tNormal.y = Vector3.Dot(((Vector3[])OtoTMatrixs[i])[1], normals[i]);
                tNormal.z = Vector3.Dot(((Vector3[])OtoTMatrixs[i])[2], normals[i]);
                normals[i] = tNormal;
            }


            return normals;

        }

        public void WriteNormalsToMesh(Mesh mesh, Vector3[] averageNormals, SmoothNormalWriteTarget writeTarget)
        {
            switch (writeTarget)
            {
                case SmoothNormalWriteTarget.VertexColor:// 写入到顶点色
                    Color[] _colors = new Color[mesh.vertexCount];
                    Color[] _colors2 = new Color[mesh.vertexCount];
                    _colors2 = mesh.colors;
                    for (var j = 0; j < mesh.vertexCount; j++)
                    {
                        //_colors[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, _colors2[j].a);
                        _colors[j] = new Vector4(1, 1, 0, 0);
                    }
                    mesh.SetColors(_colors);
                    //mesh.colors = _colors;
                    break;
                case SmoothNormalWriteTarget.Tangent://执行写入到 顶点切线
                    var tangents = new Vector4[mesh.vertexCount];
                    for (var j = 0; j < mesh.vertexCount; j++)
                    {
                        tangents[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, 0);
                    }
                    mesh.tangents = tangents;
                    break;
            }
        }

        public void ExportMesh()
        {
            GameObject[] selectObjs = Selection.gameObjects;
            int objAmount = selectObjs.Length;
            if (objAmount == 0)
            {
                Debug.LogError("no select");
                return;
            }

            foreach (GameObject o in selectObjs)
            {
                Debug.Log(o.name);
                //读取物体mesh平滑后并导出
                MeshFilter[] meshFilters = o.GetComponentsInChildren<MeshFilter>();
                SkinnedMeshRenderer[] skinMeshRenders = o.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var meshFilter in meshFilters)//遍历两种Mesh 调用平滑法线方法
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    Vector3[] averageNormals = AverageNormal(mesh);
                    Mesh newMesh = exportMesh(mesh, averageNormals, "_SMOOTH_Seele");
                    meshFilter.sharedMesh = newMesh;
                }
                foreach (var skinMeshRender in skinMeshRenders)
                {
                    Mesh mesh = skinMeshRender.sharedMesh;
                    Vector3[] averageNormals = AverageNormal(mesh);
                    Mesh newMesh = exportMesh(mesh, averageNormals, "_SMOOTH_Seele");
                    skinMeshRender.sharedMesh = newMesh;
                }

            }
        }



        public void Copy(Mesh dest, Mesh src)
        {
            dest.Clear();
            dest.vertices = src.vertices;

            List<Vector4> uvs = new List<Vector4>();

            src.GetUVs(0, uvs); dest.SetUVs(0, uvs);
            src.GetUVs(1, uvs); dest.SetUVs(1, uvs);
            src.GetUVs(2, uvs); dest.SetUVs(2, uvs);
            src.GetUVs(3, uvs); dest.SetUVs(3, uvs);

            dest.normals = src.normals;
            dest.tangents = src.tangents;
            dest.boneWeights = src.boneWeights;
            dest.colors = src.colors;
            dest.colors32 = src.colors32;
            dest.bindposes = src.bindposes;

            dest.subMeshCount = src.subMeshCount;

            for (int i = 0; i < src.subMeshCount; i++)
                dest.SetIndices(src.GetIndices(i), src.GetTopology(i), i);

            dest.name = src.name;
        }
        public Mesh exportMesh(Mesh mesh, Vector3[] averageNormals, string endName)
        {
            Mesh mesh2 = new Mesh();
            Copy(mesh2, mesh);
            switch (writeTarget)
            {
                case SmoothNormalWriteTarget.Tangent://执行写入到 顶点切线
                    Debug.Log("写入到切线中");
                    var tangents = new Vector4[mesh2.vertexCount];
                    for (var j = 0; j < mesh2.vertexCount; j++)
                    {
                        tangents[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, 0);
                    }
                    mesh2.tangents = tangents;
                    break;
                case SmoothNormalWriteTarget.VertexColor:// 写入到顶点色
                    Debug.Log("写入到顶点色中");
                    Color[] _colors = new Color[mesh2.vertexCount];

                    for (var j = 0; j < mesh2.vertexCount; j++)
                    {
                        _colors[j].r = averageNormals[j].x * 0.5f + 0.5f;
                        _colors[j].g = averageNormals[j].y * 0.5f + 0.5f;
                        _colors[j].b = averageNormals[j].z * 0.5f + 0.5f;
                        _colors[j].a = 0;
                    }
                    mesh2.colors = _colors;
                    break;
            }

            //创建文件夹路径
            string DeletePath = Application.dataPath + "/SmoothNormalTools/SeeleBlendShapes";
            Debug.Log(DeletePath);
            //判断文件夹路径是否存在
            if (!Directory.Exists(DeletePath))
            {  //创建
                Directory.CreateDirectory(DeletePath);
            }
            //刷新
            AssetDatabase.Refresh();


            mesh2.name = mesh2.name + endName;
            Debug.Log(mesh2.vertexCount);
            AssetDatabase.CreateAsset(mesh2, "Assets/SmoothNormalTools/SeeleBlendShapes/" + mesh2.name + ".asset");

            return mesh2;
        }
    }
    #endregion /* EditorWindowVer */


    public class SmoothNormalPostprocessor : AssetPostprocessor
    {
        private void OnPostprocessModel(GameObject gameObject)
        {
            SmoothNormalConfig smoothNormalAsset = SmoothNormalConfig.instance;
            if (!gameObject.name.Contains(smoothNormalAsset.smoothNormalSuffix))
                return;

            //ResourceReloader.TryReloadAllNullIn(this, SmoothNormalConfig.packagePath);
            ComputeShader smoothNormalCS = smoothNormalAsset.shaders.smoothNormalCS;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            long lastTimeStamp = 0;

            List<Mesh> meshes = new List<Mesh>();
            // Get all meshes
            {
                GameObject fbxObject = (GameObject)gameObject;

                MeshFilter[] meshFilters = fbxObject.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    meshes.Add(meshFilter.sharedMesh);
                }

                SkinnedMeshRenderer[] skinnedMeshs = fbxObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshs)
                {
                    meshes.Add(skinnedMesh.sharedMesh);
                }
            }

            // Calculate smoothNormals
            {
                GameObject fbxObject = (GameObject)gameObject;

                MeshFilter[] meshFilters = fbxObject.GetComponentsInChildren<MeshFilter>();

                foreach (Mesh mesh in meshes)
                {
                    // Init vert Color
                    Color[] vertexColors;
                    bool retainColorA = false;
                    if (mesh.colors != null && mesh.colors.Length != 0)
                    {
                        vertexColors = mesh.colors;
                        retainColorA = true;
                    }
                    else
                    {
                        vertexColors = new Color[mesh.vertexCount];
                    }
                    
                    Vector3[] smoothNormals = SmoothNormalHelper.ComputeSmoothNormal(mesh, smoothNormalCS);
                    SmoothNormalHelper.CopyVector3NormalsToColor(ref smoothNormals, ref vertexColors, vertexColors.Length, retainColorA);

                    mesh.SetColors(vertexColors);
                }
            }

            stopwatch.Stop();
            Debug.Log("Generate smoothNormal use: " + ((stopwatch.ElapsedMilliseconds - lastTimeStamp) * 0.001).ToString("F3") + "s");
        }
    }
}
#endif