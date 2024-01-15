using UnityEngine;

#if UNITY_EDITOR
namespace UnityEditor.SmoothNormalTool
{
    /// <summary>
    /// SmoothNormalConfig CustomEditor
    /// </summary>
    [CustomEditor(typeof(SmoothNormalConfig))]
    public class SmoothNormalConfigEditor : Editor
    {
        private SerializedProperty m_Shaders;
        private SerializedProperty m_MatchingMethod;
        private SerializedProperty m_MatchingNameSuffix;
        private SerializedProperty m_MatchingFilePath;
        private SerializedProperty m_WriteTarget;
        private SerializedProperty m_UserConfig;
        private SerializedProperty m_UserConfigGUID;

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
            m_UserConfigGUID = serializedObject.FindProperty(nameof(SmoothNormalConfig.userConfigGUID));

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
            // Base Properties
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
                if (m_UserConfig.boolValue)
                {
                    EditorGUILayout.PropertyField(m_UserConfigGUID);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }

            // End Button
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

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