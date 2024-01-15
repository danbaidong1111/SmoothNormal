using System;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    /// <summary>
    /// SmoothNormalConfig, default config is fixed asset in packagePath.
    /// User can create only one unique config file in the Assets/.
    /// </summary>
    public class SmoothNormalConfig : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/SmoothNormalGPU.compute")]
            public ComputeShader smoothNormalCS;
        }

        /// <summary>
        /// Import file use matching.
        /// </summary>
        public enum MatchingMethod
        {
            NameSuffix,
            FilePath
        }

        /// <summary>
        /// Write smoothNormal data to.
        /// </summary>
        public enum WriteTarget
        {
            VertexColorRGB,
            VertexColorRG,
            TangentXYZ,
            TangentXY,
        }

        /// <summary>
        /// User custom config.
        /// </summary>
        public bool useUserConfig = false;
        public string userConfigGUID = null;

        public ShaderResources shaders;

        public MatchingMethod matchingMethod = MatchingMethod.NameSuffix;
        public string matchingNameSuffix = "_SN";
        public string matchingFilePath = "Assets/SmoothNormal/";

        public WriteTarget writeTarget = WriteTarget.VertexColorRGB;

        public static readonly string packagePath = "Packages/com.danbaidong.smoothnormal";
        public static readonly string editorAssetGUID = "ddc6b06df6aa2f441a30311bae4b8d7c";

        /// <summary>
        /// Only one global instance.
        /// </summary>
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

        /// <summary>
        /// Check default file and create userConfigFile if there is no other userConfigFiles.
        /// Ping userConfigFile is there exist an userConfigFile.
        /// </summary>
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

    }
}
#endif