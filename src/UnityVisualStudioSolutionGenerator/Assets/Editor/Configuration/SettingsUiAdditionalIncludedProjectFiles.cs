#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator.Configuration
{
    /// <summary>
    ///     The part of the settings UI that handles the section for: <see cref="GeneratorSettings.AdditionalIncludedProjectFiles" />.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiAdditionalIncludedProjectFiles
    {
        private static ReorderableList? editor;

        [UserSettingBlock("General Settings")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Called by 'UserSettingsProvider'")]
        private static void AdditionalIncludedProjectFilesGui(string searchContext)
        {
            editor ??= new ReorderableList(GeneratorSettings.AdditionalIncludedProjectFilesSetting.value, typeof(string), true, false, true, true)
            {
                drawElementCallback = DrawAdditionalIncludedProjectFilesItems,
            };

            SettingsUiListHelper.DrawEditableSettingsList(
                editor,
                "C# project files (.csproj) that should be added to the generated .sln",
                GeneratorSettings.AdditionalIncludedProjectFilesSetting);
        }

        private static void DrawAdditionalIncludedProjectFilesItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            var currentValue = (string)editor!.list[index];
            if (isActive)
            {
                const int buttonWidth = 80;
                const int spaceBetweenButton = 8;
                var buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height);
                var textFieldRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - spaceBetweenButton, rect.height);
                editor.list[index] = EditorGUI.TextField(textFieldRect, currentValue);
                if (!GUI.Button(buttonRect, "Browser"))
                {
                    return;
                }

                var startDirectory = string.IsNullOrEmpty(currentValue) ? string.Empty : Path.GetDirectoryName(currentValue);
                var selectedPath = EditorUtility.OpenFilePanel("Additional C# project file", startDirectory, "csproj");
                editor.list[index] = string.IsNullOrEmpty(selectedPath) ? null : Path.GetRelativePath(Application.dataPath, selectedPath);
            }
            else
            {
                EditorGUI.LabelField(rect, currentValue);
            }
        }
    }
}
