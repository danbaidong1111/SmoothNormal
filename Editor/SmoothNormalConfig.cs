using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

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

        public enum MatchingMethod
        {
            NameSuffix,
            FilePath
        }

        public enum WriteTarget
        {
            VertexColorRGB,
            VertexColorRG,
            TangentXYZ,
            TangentXY,
        }

        public bool useUserConfig = false;
        public string userConfigGUID = null;

        public ShaderResources shaders;


        public MatchingMethod matchingMethod = MatchingMethod.NameSuffix;
        public string matchingNameSuffix = "_SN";
        public string matchingFilePath = "Assets/SmoothNormal/";

        public WriteTarget writeTarget = WriteTarget.VertexColorRGB;

        public static readonly string packagePath = "Packages/com.danbaidong.smoothnormal";
        public static readonly string editorAssetGUID = "ddc6b06df6aa2f441a30311bae4b8d7c";

        [MenuItem("Assets/Create/SmoothNormalGlobalConfig")]
        public static void CreateSmoothNormalGlobalConfig()
        {
            string resourcePath = AssetDatabase.GUIDToAssetPath(editorAssetGUID);
            var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
            SmoothNormalConfig defaultGlobalConfig = objs != null && objs.Length > 0 ? objs.First() as SmoothNormalConfig : null;

            if (defaultGlobalConfig == null)
            {
                Debug.LogError("Can't get defaultGlobalConfig guid: " + editorAssetGUID);
                return;
            }

            if (defaultGlobalConfig.useUserConfig == true && !string.IsNullOrEmpty(defaultGlobalConfig.userConfigGUID))
            {
                var userConfigFile = AssetDatabase.LoadAssetAtPath<SmoothNormalConfig>(AssetDatabase.GUIDToAssetPath(defaultGlobalConfig.userConfigGUID));
                if (userConfigFile != null)
                {
                    EditorGUIUtility.PingObject(userConfigFile);
                    Debug.LogError("The user SmoothNormalGlobalConfig file can only have one instance.");
                    return;
                }
            }

            string currentFolder = AssetDatabase.GetAssetPath(Selection.activeObject);
            var instance = CreateInstance<SmoothNormalConfig>();
            var path = string.Format(currentFolder + "/{0}.asset", typeof(SmoothNormalConfig).Name);
            ResourceReloader.ReloadAllNullIn(instance, packagePath);
            AssetDatabase.CreateAsset(instance, path);
            
            // Set package's defaultConfig properties;
            defaultGlobalConfig.useUserConfig = true;
            defaultGlobalConfig.userConfigGUID = AssetDatabase.AssetPathToGUID(path);
            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { defaultGlobalConfig }, resourcePath, true);
            s_instance = null;
        }

        private static SmoothNormalConfig s_instance;
        public static SmoothNormalConfig instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                SmoothNormalConfig globalConfig = null;

                string resourcePath = AssetDatabase.GUIDToAssetPath(editorAssetGUID);
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                globalConfig = objs != null && objs.Length > 0 ? objs.First() as SmoothNormalConfig : null;
                s_instance = globalConfig;

                if (globalConfig.useUserConfig)
                {
                    SmoothNormalConfig userConfig = null;
                    string path = AssetDatabase.GUIDToAssetPath(globalConfig.userConfigGUID);
                    userConfig = AssetDatabase.LoadAssetAtPath<SmoothNormalConfig>(path);
                    if (userConfig != null)
                    {
                        s_instance = userConfig;
                    }
                    else
                    {
                        Debug.LogError("User Config not found, back to default Config.");
                        s_instance.useUserConfig = false;
                        s_instance.userConfigGUID = null;
                        InternalEditorUtility.SaveToSerializedFileAndForget(new[] { s_instance }, resourcePath, true);
                    }
                }

                return s_instance;
            }
        }
    }

    [CustomEditor(typeof(SmoothNormalConfig))]
    public class SmoothNormalConfigEditor : Editor
    {
        private SerializedProperty m_Shaders;
        private SerializedProperty m_MatchingMethod;
        private SerializedProperty m_MatchingNameSuffix;
        private SerializedProperty m_MatchingFilePath;
        private SerializedProperty m_WriteTarget;
        private SerializedProperty m_UserConfig;

        private string curGuid = null;
        private bool isDirty = false;

        private void OnEnable()
        {
            m_Shaders = serializedObject.FindProperty(nameof(SmoothNormalConfig.shaders));
            m_MatchingMethod = serializedObject.FindProperty(nameof(SmoothNormalConfig.matchingMethod));
            m_MatchingNameSuffix = serializedObject.FindProperty(nameof(SmoothNormalConfig.matchingNameSuffix));
            m_MatchingFilePath = serializedObject.FindProperty(nameof(SmoothNormalConfig.matchingFilePath));
            m_WriteTarget = serializedObject.FindProperty(nameof(SmoothNormalConfig.writeTarget));
            m_UserConfig = serializedObject.FindProperty(nameof(SmoothNormalConfig.useUserConfig));

            string assetPath = AssetDatabase.GetAssetPath(target);
            curGuid = AssetDatabase.AssetPathToGUID(assetPath);

            isDirty = false;
        }
        private void OnDisable()
        {
            isDirty = false;
        }
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_Shaders);

            EditorGUILayout.PropertyField(m_MatchingMethod);
            if (m_MatchingMethod.intValue == (int)SmoothNormalConfig.MatchingMethod.NameSuffix)
            {
                EditorGUILayout.PropertyField(m_MatchingNameSuffix);
            }
            else
            {
                EditorGUILayout.PropertyField(m_MatchingFilePath);
            }            
            EditorGUILayout.PropertyField(m_WriteTarget);

            if (!string.IsNullOrEmpty(curGuid) && curGuid.Equals(SmoothNormalConfig.editorAssetGUID))
            {
                EditorGUILayout.PropertyField(m_UserConfig);
            }

            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Test"))
            {
                string resourcePath = AssetDatabase.GUIDToAssetPath(SmoothNormalConfig.editorAssetGUID);
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                var instance = objs != null && objs.Length > 0 ? objs.First() as SmoothNormalConfig : null;

                Debug.Log("useUserConfig: " + instance.useUserConfig);

                Debug.Log("userConfigGUID: " + instance.userConfigGUID);

                Debug.Log("Current Config: " + "sufix: " + SmoothNormalConfig.instance.matchingNameSuffix);
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

                isDirty = false;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
    }
}
#endif