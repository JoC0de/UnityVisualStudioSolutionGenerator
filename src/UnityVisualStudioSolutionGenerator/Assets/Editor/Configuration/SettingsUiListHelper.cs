#nullable enable

using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    public static class SettingsUiListHelper
    {
        public static void DrawEditableSettingsList(ReorderableList editor, string? headerText, IUserSetting setting)
        {
            _ = editor ?? throw new ArgumentNullException(nameof(editor));
            _ = setting ?? throw new ArgumentNullException(nameof(setting));

            EditorGUI.BeginChangeCheck();

            if (!string.IsNullOrEmpty(headerText))
            {
                GUILayout.Label(headerText);
            }

            EditorGUI.BeginChangeCheck();
            editor.list = (IList)setting.GetValue(); // ensure list is correct reverence e.g. when it is retested
            editor.DoLayoutList();

            // Because List is a reference type, we need to apply the changes to the backing repository
            if (EditorGUI.EndChangeCheck())
            {
                setting.ApplyModifiedProperties();
            }

            SettingsGUILayout.DoResetContextMenuForLastRect(setting);

            if (EditorGUI.EndChangeCheck())
            {
                GeneratorSettingsManager.Save();
            }
        }
    }
}
