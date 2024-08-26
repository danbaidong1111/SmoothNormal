using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    /// <summary>
    /// Hook into the import pipeline, compute smoothNormal and write it to mesh data.
    /// Note that we only changed UNITY's mesh data, the original modelfile has not changed.
    /// </summary>
    public class SmoothNormalPostprocessor : AssetPostprocessor
    {
        private static int s_DISTANCE_THRESHOLD = Shader.PropertyToID("_DISTANCE_THRESHOLD");
        /// <summary>
        /// After importing model.
        /// </summary>
        /// <param name="gameObject"></param>
        private void OnPostprocessModel(GameObject gameObject)
        {
            SmoothNormalConfig config = SmoothNormalConfig.instance;

            // Matching file
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
            smoothNormalCS.SetFloat(s_DISTANCE_THRESHOLD, config.vertDistThresold);

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

            // Compute smoothNormals
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
            Debug.Log("Generate " + gameObject.name + " smoothNormal use: " + ((stopwatch.ElapsedMilliseconds - lastTimeStamp) * 0.001).ToString("F3") + "s");
        }
    }
}
#endif