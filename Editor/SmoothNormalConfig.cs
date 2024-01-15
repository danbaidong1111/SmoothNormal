using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.ReloadAttribute;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    public class SmoothNormalConfig : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/SmoothNormalGPU.compute")]
            public ComputeShader smoothNormalCS;
        }

        public ShaderResources shaders;

        public string smoothNormalSuffix = "_SN";
        public static readonly string packagePath = "Packages/com.danbaidong.smoothnormal";
        public static readonly string editorAssetGUID = "ddc6b06df6aa2f441a30311bae4b8d7c";

        [MenuItem("Assets/Create/SmoothNormalAsset")]
        static void CreateSmoothNormalAsset()
        {
            var instance = CreateInstance<SmoothNormalConfig>();
            ResourceReloader.ReloadAllNullIn(instance, packagePath);
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(SmoothNormalConfig).Name));
        }

        private static SmoothNormalConfig s_instance;
        public static SmoothNormalConfig instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                string resourcePath = AssetDatabase.GUIDToAssetPath(editorAssetGUID);
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                s_instance = objs != null && objs.Length > 0 ? objs.First() as SmoothNormalConfig : null;

                return s_instance;
            }
        }

        public static void ReloadConfig()
        {
            Debug.Log("ReloadConfig");
        }
    }

    [CustomEditor(typeof(SmoothNormalConfig))]
    public class SmoothNormalConfigEditor : Editor
    {
        private SerializedProperty m_SmoothNormalSuffix;
        private SerializedProperty m_Shaders;

        private bool isDirty = false;

        private void OnEnable()
        {
            m_SmoothNormalSuffix = serializedObject.FindProperty(nameof(SmoothNormalConfig.smoothNormalSuffix));
            m_Shaders = serializedObject.FindProperty(nameof(SmoothNormalConfig.shaders));

            isDirty = false;
        }
        private void OnDisable()
        {
            isDirty = false;
        }
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_SmoothNormalSuffix);
            EditorGUILayout.PropertyField(m_Shaders);

            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Test"))
            {
                Debug.Log(((SmoothNormalConfig)serializedObject.targetObject).smoothNormalSuffix + ", " + SmoothNormalConfig.instance.smoothNormalSuffix);
            }

            EditorGUI.BeginDisabledGroup(!isDirty);
            if (GUILayout.Button("Revert"))
            {
                serializedObject.Update();
                isDirty = false;
            }

            if (GUILayout.Button("Apply"))
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();



                SmoothNormalConfig.ReloadConfig();

                isDirty = false;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
    }
}
#endif