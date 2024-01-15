using UnityEngine;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    public class SmoothNormalPostprocessor : AssetPostprocessor
    {
        private void OnPostprocessModel(GameObject gameObject)
        {
            SmoothNormalConfig config = SmoothNormalConfig.instance;

            switch (config.matchingMethod)
            {
                case SmoothNormalConfig.MatchingMethod.NameSuffix:
                    if (!gameObject.name.Contains(config.matchingNameSuffix))
                        return;
                    break;
                case SmoothNormalConfig.MatchingMethod.FilePath:
                    string path = assetImporter.assetPath;
                    if (!path.Contains(config.matchingFilePath))
                        return;
                    break;
                default:
                    return;
            }

            ComputeShader smoothNormalCS = config.shaders.smoothNormalCS;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            long lastTimeStamp = 0;

            List<Mesh> meshes = new List<Mesh>();
            // Get all meshes
            {
                MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    meshes.Add(meshFilter.sharedMesh);
                }

                SkinnedMeshRenderer[] skinnedMeshs = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshs)
                {
                    meshes.Add(skinnedMesh.sharedMesh);
                }
            }

            // Calculate smoothNormals
            {
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

                    Vector4[] tangents = mesh.tangents;
                    switch (config.writeTarget)
                    {
                        case SmoothNormalConfig.WriteTarget.VertexColorRGB:
                            SmoothNormalHelper.CopyVector3NormalsToColorRGB(ref smoothNormals, ref vertexColors, vertexColors.Length, retainColorA);
                            mesh.SetColors(vertexColors);
                            break;
                        case SmoothNormalConfig.WriteTarget.VertexColorRG:
                            SmoothNormalHelper.CopyVector3NormalsToColorRG(ref smoothNormals, ref vertexColors, vertexColors.Length, retainColorA);
                            mesh.SetColors(vertexColors);
                            break;
                        case SmoothNormalConfig.WriteTarget.TangentXYZ:
                            SmoothNormalHelper.CopyVector3NormalsToTangentXYZ(ref smoothNormals, ref tangents, vertexColors.Length);
                            mesh.SetTangents(tangents);
                            break;
                        case SmoothNormalConfig.WriteTarget.TangentXY:
                            SmoothNormalHelper.CopyVector3NormalsToTangentXY(ref smoothNormals, ref tangents, vertexColors.Length);
                            mesh.SetTangents(tangents);
                            break;
                    }

                }
            }

            stopwatch.Stop();
            Debug.Log("Generate smoothNormal use: " + ((stopwatch.ElapsedMilliseconds - lastTimeStamp) * 0.001).ToString("F3") + "s");
        }
    }
}
#endif