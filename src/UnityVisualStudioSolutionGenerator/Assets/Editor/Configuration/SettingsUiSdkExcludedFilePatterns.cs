#nullable enable

using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiSdkExcludedFilePatterns
    {
        private static ReorderableList? editor;

        [UserSettingBlock("Sdk-style Project Settings")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        private static void SdkExcludedFilePatternsGui(string searchContext)
        {
            editor ??= new ReorderableList(GeneratorSettings.SdkExcludedFilePatternsSetting.value, typeof(string), false, false, true, true)
            {
                drawElementCallback = DrawSdkExcludedFilePatternsItems,
            };

            SettingsUiListHelper.DrawEditableSettingsList(editor, "Excluded files patterns", GeneratorSettings.SdkExcludedFilePatternsSetting);
        }

        private static void DrawSdkExcludedFilePatternsItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (isActive)
            {
                editor!.list[index] = EditorGUI.TextField(rect, (string)editor.list[index]);
            }
            else
            {
                EditorGUI.LabelField(rect, (string)editor!.list[index]);
            }
        }
    }
}
