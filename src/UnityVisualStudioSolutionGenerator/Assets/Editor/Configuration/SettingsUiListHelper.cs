#nullable enable

using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Helper for drawing <see cref="ReorderableList" />s in the settings UI.
    /// </summary>
    internal static class SettingsUiListHelper
    {
        /// <summary>
        ///     Draws a <see cref="ReorderableList" /> in the settings UI.
        /// </summary>
        /// <param name="editor">The 'list' to draw.</param>
        /// <param name="headerText">An additional header text to show, if omitted no header will by rendered.</param>
        /// <param name="setting">The settings value that is edited by this UI, used to apply / persist changes done by the UI.</param>
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
