using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    [ExecuteAlways]
    public class DrawNormalGizmos : MonoBehaviour
    {
        public ShowNormalDataFrom normalDataFrom = ShowNormalDataFrom.VertexColor;
        public float lineLength = 0.1f;
        private float _lineLengthCache = 0.1f;
        private Mesh _mesh;
        private Color[] m_ColorCache;
        private ShowNormalDataFrom normalDataFromCache;

        public enum ShowNormalDataFrom
        {
            VertexNormal,
            VertexColor,
        }
        struct NormalLine
        {
            public Vector3 posFrom;
            public Vector3 normalWS;
        }

        private List<NormalLine> _normalLines;

        void CalculateNormalLine()
        {
            _normalLines.Clear();
            if (_mesh != null)
            {
                for (int i = 0; i < _mesh.colors.Length; i++)
                {
                    var normalLine = new NormalLine();
                    var mat = transform.localToWorldMatrix;

                    Vector3 normalOS = _mesh.normals[i];
                    if (normalDataFrom == ShowNormalDataFrom.VertexColor)
                    {
                        Vector3 normalTS = new Vector3(_mesh.colors[i].r, _mesh.colors[i].g, _mesh.colors[i].b) * 2.0f - Vector3.one;

                        var tangent = _mesh.tangents[i];
                        var normal = _mesh.normals[i];
                        var binormal = (Vector3.Cross(normal, tangent) * tangent.w).normalized;
                        var TBNMatrix = new Matrix4x4(tangent, binormal, normal, Vector4.zero);
                        normalOS = TBNMatrix.MultiplyVector(normalTS).normalized;
                    }

                    normalLine.posFrom = mat.MultiplyPoint(_mesh.vertices[i]);
                    normalLine.normalWS = mat.MultiplyVector(normalOS);
                    _normalLines.Add(normalLine);
                }
            }
            normalDataFromCache = normalDataFrom;
            _lineLengthCache = lineLength;
            m_ColorCache = _mesh.colors;
        }
        void OnEnable()
        {
            _normalLines = new List<NormalLine>();
            if (TryGetComponent<MeshFilter>(out MeshFilter filter))
                _mesh = filter.sharedMesh;
            else
                _mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;

            CalculateNormalLine();
        }

        private void Update()
        {

        }

        private void OnDisable()
        {
            _mesh = null;
            _normalLines = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            if (_mesh != null)
            {
                if (normalDataFromCache != normalDataFrom)
                    CalculateNormalLine();

                foreach (var normalLine in _normalLines)
                {
                    Gizmos.DrawLine(normalLine.posFrom, normalLine.posFrom + normalLine.normalWS * lineLength);
                }
            }
        }
    }
}

#endif