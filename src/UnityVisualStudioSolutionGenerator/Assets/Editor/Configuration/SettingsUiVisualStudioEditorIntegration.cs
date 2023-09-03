#nullable enable
using System.Diagnostics.CodeAnalysis;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.CodeEditor;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     The part of the settings UI that integrates the settings from the Unity visual studio editor plugin.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiVisualStudioEditorIntegration
    {
        [UserSettingBlock("Visual Studio Editor integration")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Called by 'UserSettingsProvider'")]
        private static void VisualStudioEditorSettingsGui(string searchContext)
        {
            if (CodeEditor.CurrentEditor is VisualStudioEditor visualStudioEditor)
            {
                visualStudioEditor.OnGUI();
            }
        }
    }
}
