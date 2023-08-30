#nullable enable

using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiExcludedAnalyzers
    {
        private static ReorderableList? editor;

        [UserSettingBlock("General Settings")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        private static void ExcludedAnalyzersGui(string searchContext)
        {
            editor ??= new ReorderableList(GeneratorSettings.ExcludedAnalyzers, typeof(string), false, true, true, true)
            {
                drawHeaderCallback = DrawHeader, drawElementCallback = DrawItems,
            };

            SettingsUiListHelper.DrawEditableSettingsList(
                editor,
                "Analyzers to exclude from Project (e.g. because they are not loading)",
                GeneratorSettings.ExcludedAnalyzersSetting);
            EditorGUI.BeginChangeCheck();
        }

        private static void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Analyzer pattern (can contain '*')");
        }

        private static void DrawItems(Rect rect, int index, bool isActive, bool isFocused)
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
