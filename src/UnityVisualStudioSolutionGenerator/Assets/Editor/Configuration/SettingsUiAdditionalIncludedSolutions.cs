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
    ///     The part of the settings UI that handles the section for: <see cref="GeneratorSettings.AdditionalIncludedSolutions" />.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiAdditionalIncludedSolutions
    {
        private static ReorderableList? editor;

        [UserSettingBlock("General Settings")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Called by 'UserSettingsProvider'")]
        private static void AdditionalIncludedSolutionsGui(string searchContext)
        {
            editor ??= new ReorderableList(GeneratorSettings.AdditionalIncludedSolutionsSetting.value, typeof(string), true, false, true, true)
            {
                drawElementCallback = DrawAdditionalIncludedSolutionsItems,
            };

            SettingsUiListHelper.DrawEditableSettingsList(
                editor,
                "Visual studio solutions (.sln) of which the projects should be added to the generated .sln",
                GeneratorSettings.AdditionalIncludedSolutionsSetting);
        }

        private static void DrawAdditionalIncludedSolutionsItems(Rect rect, int index, bool isActive, bool isFocused)
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
                editor.list[index] = EditorUtility.OpenFilePanel("Additional visual studio solutions", startDirectory, "sln");
            }
            else
            {
                EditorGUI.LabelField(rect, currentValue);
            }
        }
    }
}
