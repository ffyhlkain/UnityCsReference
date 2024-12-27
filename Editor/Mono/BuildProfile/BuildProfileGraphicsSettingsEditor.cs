// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.Build.Profile
{
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    [CustomEditor(typeof(BuildProfileGraphicsSettings))]
    class BuildProfileGraphicsSettingsEditor : Editor
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileCommonGraphicsSettings.uxml";
        const string k_StyleSheet = "BuildProfile/StyleSheets/BuildProfile.uss";
        const string k_LastDefaultPropertyPath = "m_EditorClassIdentifier";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            root.Bind(serializedObject);
            root.Query<UIElements.ProjectSettings.ProjectSettingsElementWithSO>()
                .ForEach(d => d.InitializeWithoutWindow(serializedObject));

            BindEnumFieldWithFadeGroup(root, "Lightmap", CalculateLightmapStrippingFromCurrentScene);
            BindEnumFieldWithFadeGroup(root, "Fog", CalculateFogStrippingFromCurrentScene);

            // Align fields as in the inspector
            var type = typeof(BaseField<>);
            root.Query<BindableElement>()
                .Where(e =>
                    IsSubclassOfGeneric(type, e.GetType()))
                .ForEach(e =>
                    e.EnableInClassList(BaseField<bool>.alignedFieldUssClassName, true));

            return root;
        }

        void BindEnumFieldWithFadeGroup(VisualElement content, string id, Action buttonCallback)
        {
            var enumMode = content.MandatoryQ<EnumField>($"{id}Modes");
            var enumModeGroup = content.MandatoryQ<VisualElement>($"{id}ModesGroup");
            var enumModeProperty = serializedObject.FindProperty($"m_{id}Stripping");

            static bool IsModesGroupVisible(StrippingModes mode) => mode == StrippingModes.Custom;
            UIElementsEditorUtility.SetVisibility(enumModeGroup, IsModesGroupVisible((StrippingModes)enumModeProperty.intValue));
            var lightmapModesUpdate = UIElementsEditorUtility.BindSerializedProperty<StrippingModes>(enumMode, enumModeProperty,
                mode => UIElementsEditorUtility.SetVisibility(enumModeGroup, IsModesGroupVisible(mode)));
            lightmapModesUpdate?.Invoke();

            content.MandatoryQ<Button>($"Import{id}FromCurrentScene").clicked += buttonCallback;
        }

        void CalculateLightmapStrippingFromCurrentScene()
        {
            bool lightmapKeepPlain, lightmapKeepDirCombined, lightmapKeepDynamicPlain, lightmapKeepDynamicDirCombined, lightmapKeepShadowMask, lightmapKeepSubtractive;
            ShaderUtil.CalculateLightmapStrippingFromCurrentSceneForBuildProfile(out lightmapKeepPlain, out lightmapKeepDirCombined,
                out lightmapKeepDynamicPlain, out lightmapKeepDynamicDirCombined, out lightmapKeepShadowMask, out lightmapKeepSubtractive);

            serializedObject.FindProperty("m_LightmapKeepPlain").boolValue = lightmapKeepPlain;
            serializedObject.FindProperty("m_LightmapKeepDirCombined").boolValue = lightmapKeepDirCombined;
            serializedObject.FindProperty("m_LightmapKeepDynamicPlain").boolValue = lightmapKeepDynamicPlain;
            serializedObject.FindProperty("m_LightmapKeepDynamicDirCombined").boolValue = lightmapKeepDynamicDirCombined;
            serializedObject.FindProperty("m_LightmapKeepShadowMask").boolValue = lightmapKeepShadowMask;
            serializedObject.FindProperty("m_LightmapKeepSubtractive").boolValue = lightmapKeepSubtractive;

            serializedObject.ApplyModifiedProperties();
        }

        void CalculateFogStrippingFromCurrentScene()
        {
            bool fogKeepLinear, fogKeepExp, fogKeepExp2;
            ShaderUtil.CalculateFogStrippingFromCurrentSceneForBuildProfile(out fogKeepLinear, out fogKeepExp, out fogKeepExp2);

            serializedObject.FindProperty("m_FogKeepLinear").boolValue = fogKeepLinear;
            serializedObject.FindProperty("m_FogKeepExp").boolValue = fogKeepExp;
            serializedObject.FindProperty("m_FogKeepExp2").boolValue = fogKeepExp2;

            serializedObject.ApplyModifiedProperties();
        }

        static bool IsSubclassOfGeneric(Type genericType, Type typeToCheck) {
            while (typeToCheck != null && typeToCheck != typeof(object))
            {
                var currentType = typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck;
                if (genericType == currentType)
                    return true;
                typeToCheck = typeToCheck.BaseType;
            }
            return false;
        }

        public bool IsDataEqualToGlobalGraphicsSettings()
        {
            var globalGraphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var globalGraphicsSettingsSO = new SerializedObject(globalGraphicsSettings);
            var profileSerializedProperty = serializedObject.FindProperty(k_LastDefaultPropertyPath);

            while (profileSerializedProperty.Next(false))
            {
                var globalSerializedProperty = globalGraphicsSettingsSO.FindProperty(profileSerializedProperty.name);
                if (profileSerializedProperty.isArray)
                {
                    if (profileSerializedProperty.arraySize != globalSerializedProperty.arraySize)
                        return false;

                    for (int i = 0; i < profileSerializedProperty.arraySize; i++)
                    {
                        var profileArrayElement = profileSerializedProperty.GetArrayElementAtIndex(i);
                        var globalArrayElement = globalSerializedProperty.GetArrayElementAtIndex(i);
                        if (profileArrayElement.boxedValue != globalArrayElement.boxedValue)
                            return false;
                    }
                }
                else if (!profileSerializedProperty.boxedValue.Equals(globalSerializedProperty.boxedValue))
                    return false;
            }

            return true;
        }

        public void ResetToGlobalGraphicsSettingsValues()
        {
            var globalGraphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var globalGraphicsSettingsSO = new SerializedObject(globalGraphicsSettings);
            var profileSerializedProperty = serializedObject.FindProperty(k_LastDefaultPropertyPath);

            while (profileSerializedProperty.Next(false))
            {
                var globalSerializedProperty = globalGraphicsSettingsSO.FindProperty(profileSerializedProperty.name);
                if (profileSerializedProperty.isArray)
                {
                    profileSerializedProperty.arraySize = globalSerializedProperty.arraySize;
                    for (int i = 0; i < profileSerializedProperty.arraySize; i++)
                    {
                        var profileArrayElement = profileSerializedProperty.GetArrayElementAtIndex(i);
                        var globalArrayElement = globalSerializedProperty.GetArrayElementAtIndex(i);
                        profileArrayElement.boxedValue = globalArrayElement.boxedValue;
                    }
                }
                else
                    profileSerializedProperty.boxedValue = globalSerializedProperty.boxedValue;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
