using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public class MeshVertexManager : EditorWindow
{
    [MenuItem("Tools/MeshVertexManager", false, 2)]
    private static void MeshVertexManagerWindow()
    {
        GetWindow<MeshVertexManager>("Mesh Vertex Manager");
    }

    private static bool ShowVertexIndeces;
    private static int fontSize = 8;
    private static Color fontColor = Color.blue;
    private Mesh meshtemplate;
    private string filePath;
    private ProcessWay way = ProcessWay.UnifyIndecesAndTriangles;
    private string toDoPath = "Assets/MeshManager/ModelToProcess/";
    private string DonePth = "Assets/MeshManager/ModelProcessed/";

    private void OnGUI()
    {
        ShowVertex();
        MeshManager();
    }

    private void ShowVertex()
    {
        EditorGUILayout.LabelField("参数设置:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        ShowVertexIndeces = EditorGUILayout.Toggle("显示顶点序列", ShowVertexIndeces);
        if (ShowVertexIndeces)
        {
            fontColor = EditorGUILayout.ColorField("字体颜色", fontColor);
            fontSize = EditorGUILayout.IntField("字体大小", fontSize);
        }

        EditorGUILayout.EndVertical();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void MeshManager()
    {
        EditorGUILayout.LabelField("对齐点序:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        meshtemplate = (Mesh)EditorGUILayout.ObjectField("模板模型", meshtemplate, typeof(Mesh));
        way = (ProcessWay)EditorGUILayout.EnumPopup("点序对齐方式", way);
        EditorGUILayout.LabelField("待处理路径:" + toDoPath);
        EditorGUILayout.LabelField("已处理路径:" + DonePth);
        if (GUILayout.Button("对齐点序"))
        {
            ProcessMesh();
        }

        EditorGUILayout.EndVertical();
    }

    enum ProcessWay
    {
        UnifyOnlyVertexIndeces,
        UnifyIndecesAndTriangles
    }

    void ProcessMesh()
    {
        if (Directory.Exists(toDoPath))
        {
            string[] files = Directory.GetFiles(toDoPath);

            foreach (string file in files)
            {
                if (file.EndsWith(".meta")) continue; // Skip .meta files
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                Mesh toDoMesh = asset.GetComponent<MeshFilter>().sharedMesh;
                toDoMesh.name = asset.name;
                Mesh unifiedMesh;
                switch (way)
                {
                    case ProcessWay.UnifyIndecesAndTriangles:
                        unifiedMesh = UnifyIndicesAndTriangles(toDoMesh, meshtemplate);
                        CreateMeshAndSave(unifiedMesh);
                        break;
                    case ProcessWay.UnifyOnlyVertexIndeces:
                        unifiedMesh = UnifyOnlyVertexIndeces(toDoMesh, meshtemplate);
                        CreateMeshAndSave(unifiedMesh);
                        break;
                }
            }

            Mesh UnifyIndicesAndTriangles(Mesh mesh, Mesh meshTemplate)
            {
                // 根据UV遍历的顶点序列
                int[] vertexIndices = new int[mesh.vertexCount];
                Vector2[] meshUVs = mesh.uv;
                Vector2[] meshTemplateUVs = meshTemplate.uv;
                Dictionary<int, Vector2> dict_meshTemplate_indexToUV = new Dictionary<int, Vector2>();

                for (int i = 0; i < meshTemplateUVs.Length; i++)
                {
                    dict_meshTemplate_indexToUV.Add(i, meshTemplateUVs[i]);
                }

                for (int i = 0; i < meshUVs.Length; i++)
                {
                    int index = -1;
                    foreach (KeyValuePair<int, Vector2> kvp in dict_meshTemplate_indexToUV)
                    {
                        if (Vector2.SqrMagnitude(meshUVs[i] - kvp.Value) < 0.000000001f) // 用平方距离进行比较
                        {
                            index = kvp.Key;
                            break;
                        }
                    }

                    vertexIndices[i] = index;
                }

                Vector3[] orderedVertices = new Vector3[meshTemplate.vertexCount];
                for (int i = 0; i < vertexIndices.Length; i++)
                {
                    orderedVertices[vertexIndices[i]] = mesh.vertices[i];
                }

                Mesh orderedMesh = new Mesh();
                orderedMesh.name = mesh.name;
                orderedMesh.vertices = orderedVertices;
                orderedMesh.triangles = meshTemplate.triangles;
                orderedMesh.normals = meshTemplate.normals;
                orderedMesh.uv = meshTemplate.uv;
                orderedMesh.RecalculateBounds();
                orderedMesh.RecalculateTangents();

                return orderedMesh;
            }

            Mesh UnifyOnlyVertexIndeces(Mesh mesh, Mesh meshTemplate)
            {
                // 根据UV遍历的顶点序列
                int[] vertexIndices = new int[mesh.vertexCount];
                Vector2[] meshUVs = mesh.uv;
                Vector2[] meshTemplateUVs = meshTemplate.uv;
                Dictionary<int, Vector2> dict_meshTemplate_indexToUV = new Dictionary<int, Vector2>();
                for (int i = 0; i < meshTemplateUVs.Length; i++)
                {
                    dict_meshTemplate_indexToUV.Add(i, meshTemplateUVs[i]);
                }

                for (int i = 0; i < meshUVs.Length; i++)
                {
                    int index = -1;
                    foreach (KeyValuePair<int, Vector2> kvp in dict_meshTemplate_indexToUV)
                    {
                        if (Vector3.Distance(meshUVs[i], kvp.Value) <= 0.0001f)
                        {
                            index = kvp.Key;
                            break;
                        }
                    }

                    vertexIndices[i] = index;
                }

                // 构建新的顶点数组和三角形索引数组
                Vector3[] newVertices = new Vector3[meshTemplate.vertexCount];
                Vector3[] newNormals = new Vector3[meshTemplate.vertexCount];
                Vector2[] newUVs = new Vector2[meshTemplate.vertexCount];
                List<int> newTriangles = new List<int>();

                // 填充新的顶点数组
                for (int i = 0; i < vertexIndices.Length; i++)
                {
                    int templateIndex = vertexIndices[i];
                    if (templateIndex != -1)
                    {
                        newVertices[templateIndex] = mesh.vertices[i];
                        newNormals[templateIndex] = mesh.normals[i];
                        newUVs[templateIndex] = mesh.uv[i];
                    }
                }

                // 构建新的三角形索引数组
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    int index0 = mesh.triangles[i];
                    int index1 = mesh.triangles[i + 1];
                    int index2 = mesh.triangles[i + 2];

                    int templateIndex0 = vertexIndices[index0];
                    int templateIndex1 = vertexIndices[index1];
                    int templateIndex2 = vertexIndices[index2];

                    if (templateIndex0 != -1 && templateIndex1 != -1 && templateIndex2 != -1)
                    {
                        newTriangles.Add(templateIndex0);
                        newTriangles.Add(templateIndex1);
                        newTriangles.Add(templateIndex2);
                    }
                }

                Mesh orderedMesh = new Mesh();
                orderedMesh.name = mesh.name;
                orderedMesh.vertices = newVertices;
                orderedMesh.normals = newNormals;
                orderedMesh.uv = newUVs;
                orderedMesh.triangles = newTriangles.ToArray();
                orderedMesh.RecalculateBounds();
                //orderedMesh.RecalculateNormals();
                orderedMesh.RecalculateTangents();
                return orderedMesh;
            }

            void CreateMeshAndSave(Mesh orderedMesh)
            {
                if (!AssetDatabase.IsValidFolder(DonePth))
                {
                    AssetDatabase.CreateFolder("Assets/MeshManager", "ModelProcessed");
                }

                AssetDatabase.CreateAsset(orderedMesh, DonePth + "/" + orderedMesh.name + ".asset");
            }

            [DrawGizmo(GizmoType.Selected)]
            static void ShowVertexIndices(Transform objectTransform, GizmoType gizmoType)
            {
                if (objectTransform == null) return;
                if (!ShowVertexIndeces) return;

                MeshFilter meshFilter = objectTransform.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) return;

                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                Vector3[] normals = meshFilter.sharedMesh.normals;
                Camera sceneCamera = SceneView.lastActiveSceneView.camera;
                if (sceneCamera == null) return;

                GUIStyle style = new GUIStyle();
                style.fontSize = fontSize; // 设置字体大小
                style.normal.textColor = fontColor; // 设置字体颜色


                float distanceThreshold = 0.07f; // 距离阈值
                List<Vector3> displayedPoints = new List<Vector3>();

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 worldPos = objectTransform.TransformPoint(vertices[i]);
                    Vector3 worldNormal = objectTransform.TransformDirection(normals[i]);
                    Vector3 cameraDirection = (worldPos - sceneCamera.transform.position).normalized;

                    if (Vector3.Dot(worldNormal, cameraDirection) < 0.2)
                    {
                        bool tooClose = false;
                        foreach (Vector3 point in displayedPoints)
                        {
                            if (Vector3.Distance(worldPos, point) < distanceThreshold)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            Gizmos.DrawSphere(worldPos, 0.005f);
                            Handles.Label(worldPos - new Vector3(0.001f, 0.001f, 0.001f), i.ToString(), style);
                            displayedPoints.Add(worldPos);
                        }
                    }
                }
            }
        }
    }
}