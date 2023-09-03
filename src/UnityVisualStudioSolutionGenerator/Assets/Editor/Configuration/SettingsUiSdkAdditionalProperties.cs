#nullable enable

using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     The part of the settings UI that handles the section for: <see cref="GeneratorSettings.SdkAdditionalProperties" />.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiSdkAdditionalProperties
    {
        private const string PropertiesDocumentationLink = "https://learn.microsoft.com/de-de/dotnet/core/project-sdk/msbuild-props";

        private static ReorderableList? editor;

        [UserSettingBlock("Sdk-style Project Settings")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Called by 'UserSettingsProvider'")]
        private static void SdkAdditionalPropertiesGui(string searchContext)
        {
            editor ??= new ReorderableList(
                GeneratorSettings.SdkAdditionalPropertiesSetting.value,
                typeof(PropertyGroupSetting),
                true,
                true,
                true,
                true) { drawHeaderCallback = DrawHeader, drawElementCallback = DrawSdkAdditionalPropertiesItems };

            GUILayout.Label("Additional Project Properties (PropertyGroup)");
            EditorGUILayout.HelpBox(
                "This project settings are only considered by Visual Studio. Unity building is not affected by them.",
                MessageType.Info);

            GUILayout.Label("Documentation about available properties see:");
            if (EditorGUILayout.LinkButton(PropertiesDocumentationLink))
            {
                Application.OpenURL(PropertiesDocumentationLink);
            }

            SettingsUiListHelper.DrawEditableSettingsList(editor, null, GeneratorSettings.SdkAdditionalPropertiesSetting);
        }

        private static void DrawHeader(Rect rect)
        {
            const int space = 5;
            var halfSpace = (rect.width - space) / 2;
            var firstHalfRect = new Rect(rect) { width = halfSpace };
            var secondHalfRect = new Rect(rect) { width = halfSpace, x = rect.x + halfSpace + space };
            EditorGUI.LabelField(firstHalfRect, "Name");
            EditorGUI.LabelField(secondHalfRect, "Value");
        }

        private static void DrawSdkAdditionalPropertiesItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            var currentValue = (PropertyGroupSetting)editor!.list[index];
            const int space = 5;
            var halfSpace = (rect.width - space) / 2;
            var firstHalfRect = new Rect(rect) { width = halfSpace };
            var secondHalfRect = new Rect(rect) { width = halfSpace, x = rect.x + halfSpace + space };
            if (isActive)
            {
                currentValue.Name = EditorGUI.TextField(firstHalfRect, currentValue.Name);
                currentValue.Value = EditorGUI.TextField(secondHalfRect, currentValue.Value);
            }
            else
            {
                EditorGUI.LabelField(firstHalfRect, currentValue.Name);
                EditorGUI.LabelField(secondHalfRect, currentValue.Value);
            }
        }
    }
}
