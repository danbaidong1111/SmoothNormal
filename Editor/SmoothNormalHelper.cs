using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    public class SmoothNormalHelper
    {
        public struct TriangleData
        {
            public Vector4 faceNormal;
            public Vector4 vertWeights;
            public Vector4 vertIndices;
        }

        internal static int DivRoundUp(int x, int y) => (x + y - 1) / y;

        private static Dictionary<Vector3, Vector3> CreateVertexNormalsDictionary(Mesh mesh)
        {
            Dictionary<Vector3, Vector3> vartNormalsDictionary = new Dictionary<Vector3, Vector3>();

            for (int i = 0; i <= mesh.triangles.Length - 3; i += 3)
            {
                // Get edges, cal faceNormal, angleWeights.
                Vector3 a = mesh.vertices[mesh.triangles[i + 1]] - mesh.vertices[mesh.triangles[i]];a = a.normalized;
                Vector3 b = mesh.vertices[mesh.triangles[i + 2]] - mesh.vertices[mesh.triangles[i]];b = b.normalized;
                Vector3 c = mesh.vertices[mesh.triangles[i + 2]] - mesh.vertices[mesh.triangles[i + 1]];c = c.normalized;
                Vector3 faceNormal = Vector3.Cross(a, b).normalized;
                float[] angleWeight = { Vector3.Angle(a, b),
                                        Vector3.Angle(-a, c),
                                        Vector3.Angle(b, c) };

                for (int j = 0; j < 3; j++)
                {
                    Vector3 weightedNormal;
                    int tri = mesh.triangles[i + j];
                    Vector3 vertPos = mesh.vertices[tri];

                    if (!vartNormalsDictionary.ContainsKey(vertPos))
                    {
                        vartNormalsDictionary.Add(vertPos, faceNormal * angleWeight[j]);
                    }
                    else
                    {
                        if (vartNormalsDictionary.TryGetValue(vertPos, out weightedNormal))
                        {
                            weightedNormal += (faceNormal * angleWeight[j]);
                            vartNormalsDictionary[vertPos] = weightedNormal;
                        }
                    }
                }
            }

            return vartNormalsDictionary;
        }

        private static Vector3[] CalculateAngleWeightedNormal(Dictionary<Vector3, Vector3> surfaceNormalDictionary, Mesh mesh)
        {
            List<Vector3> averageNoramls = new List<Vector3>();
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 weightedNormal = surfaceNormalDictionary[mesh.vertices[i]];

                averageNoramls.Add(weightedNormal.normalized);
            }

            return averageNoramls.ToArray();
        }

        public static Vector3[] CalculateSmoothNormal(Mesh mesh)
        {
            Dictionary<Vector3, Vector3> dic = CreateVertexNormalsDictionary(mesh);

            Vector3[] normals = CalculateAngleWeightedNormal(dic, mesh);

            //ObjectSpace to TangentSpace
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                var tangent = mesh.tangents[i];
                var normal = mesh.normals[i];
                var binormal = (Vector3.Cross(normal, tangent) * tangent.w).normalized;
                var TBNMatrix = new Matrix4x4(tangent, binormal, normal, Vector4.zero);
                TBNMatrix = TBNMatrix.transpose;

                normals[i] = TBNMatrix.MultiplyVector(normals[i]).normalized;
                normals[i] = normals[i] * 0.5f + Vector3.one * 0.5f;
            }

            return normals;
        }

        public static Vector3[] ComputeSmoothNormal(Mesh mesh, ComputeShader cs)
        {
            // Compute
            float4[] smoothNormalsArray = new float4[mesh.normals.Length];
            float4[] vertPosArray = new float4[mesh.vertices.Length];
            float4[] vertNormalsArray = new float4[mesh.normals.Length];
            float4[] vertTangentsArray = new float4[mesh.tangents.Length];
            TriangleData[] triangleDataArray = new TriangleData[mesh.triangles.Length / 3];

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                vertPosArray[i] = new float4(mesh.vertices[i], i);
                vertNormalsArray[i] = new float4(mesh.normals[i], 0);
                vertTangentsArray[i] = mesh.tangents[i];
            }

            ComputeBuffer vertPosBuffer = new ComputeBuffer(vertPosArray.Length, sizeof(float) * 4);
            ComputeBuffer vertNormalsBuffer = new ComputeBuffer(vertNormalsArray.Length, sizeof(float) * 4);
            ComputeBuffer vertTangentsBuffer = new ComputeBuffer(vertTangentsArray.Length, sizeof(float) * 4);
            ComputeBuffer trianglesBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
            ComputeBuffer triangleDataBuffer = new ComputeBuffer(triangleDataArray.Length, sizeof(float) * 4 * 3);
            ComputeBuffer smoothNormalsBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 4);

            vertPosBuffer.SetData(vertPosArray);
            vertNormalsBuffer.SetData(vertNormalsArray);
            vertTangentsBuffer.SetData(vertTangentsArray);
            trianglesBuffer.SetData(mesh.triangles);
            triangleDataBuffer.SetData(triangleDataArray);
            smoothNormalsBuffer.SetData(smoothNormalsArray);

            cs.SetInt("_VerticesCounts", mesh.vertices.Length);
            cs.SetInt("_TrianglesCounts", mesh.triangles.Length);

            cs.SetBuffer(0, "_VertPosBuffer", vertPosBuffer);
            cs.SetBuffer(0, "_TrianglesBuffer", trianglesBuffer);
            cs.SetBuffer(0, "_TriangleDataBuffer", triangleDataBuffer);
            cs.Dispatch(0, DivRoundUp(mesh.triangles.Length / 3, 64), 1, 1);

            cs.SetBuffer(1, "_VertPosBuffer", vertPosBuffer);
            cs.SetBuffer(1, "_VertNormalsBuffer", vertNormalsBuffer);
            cs.SetBuffer(1, "_VertTangentsBuffer", vertTangentsBuffer);
            cs.SetBuffer(1, "_TriangleDataBuffer", triangleDataBuffer);
            cs.SetBuffer(1, "_SmoothNormalsBuffer", smoothNormalsBuffer);
            cs.Dispatch(1, DivRoundUp(mesh.vertices.Length, 64), 1, 1);

            smoothNormalsBuffer.GetData(smoothNormalsArray);

            //string debugstr = "TriNum: " + mesh.triangles.Length + ", DispatchNum: " + DivRoundUp(mesh.triangles.Length / 3, 64)
            //                + ", vertNum: " + mesh.vertices.Length + "\n";
            //foreach (float4 f3 in smoothNormalsArray)
            //{
            //    debugstr += f3 + "\n";
            //}

            //Debug.Log(debugstr);

            // Release Resources
            vertPosBuffer?.Release();
            vertNormalsBuffer.Release();
            vertTangentsBuffer.Release();
            trianglesBuffer?.Release();
            triangleDataBuffer?.Release();
            smoothNormalsBuffer?.Release();

            Vector3[] smoothNormals = new Vector3[mesh.normals.Length];
            for (int i = 0; i < smoothNormals.Length; i++)
            {
                smoothNormals[i] = new Vector3(smoothNormalsArray[i].x, smoothNormalsArray[i].y, smoothNormalsArray[i].z);
            }

            return smoothNormals;
        }

        public static void CopyVector3NormalsToColorRGB(ref Vector3[] smoothNormals, ref Color[] vertexColors, int size, bool retainColorA)
        {
            if (retainColorA)
            {
                for (int i = 0; i < size; i++)
                {
                    vertexColors[i] = new Vector4(smoothNormals[i].x, smoothNormals[i].y, smoothNormals[i].z, vertexColors[i].a);
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    vertexColors[i] = new Vector4(smoothNormals[i].x, smoothNormals[i].y, smoothNormals[i].z, 1);
                }
            }
        }

        public static void CopyVector3NormalsToColorRG(ref Vector3[] smoothNormals, ref Color[] vertexColors, int size, bool retainColorA)
        {
            if (retainColorA)
            {
                for (int i = 0; i < size; i++)
                {
                    vertexColors[i] = new Vector4(smoothNormals[i].x, smoothNormals[i].y, vertexColors[i].b, vertexColors[i].a);
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    vertexColors[i] = new Vector4(smoothNormals[i].x, smoothNormals[i].y, 1, 1);
                }
            }
        }

        public static void CopyVector3NormalsToTangentXYZ(ref Vector3[] smoothNormals, ref Vector4[] vertexTangents, int size)
        {
            for (int i = 0; i < size; i++)
            {
                vertexTangents[i].x = smoothNormals[i].x;
                vertexTangents[i].y = smoothNormals[i].y;
                vertexTangents[i].z = smoothNormals[i].z;
            }
        }

        public static void CopyVector3NormalsToTangentXY(ref Vector3[] smoothNormals, ref Vector4[] vertexTangents, int size)
        {
            for (int i = 0; i < size; i++)
            {
                vertexTangents[i].x = smoothNormals[i].x;
                vertexTangents[i].y = smoothNormals[i].y;
            }
        }
    }

}
#endif