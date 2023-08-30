#nullable enable
using System.Diagnostics.CodeAnalysis;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.CodeEditor;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Used by 'UserSettingsProvider'")]
    public static class SettingsUiVisualStudioEditorIntegration
    {
        [UserSettingBlock("Visual Studio Editor integration")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by 'UserSettingsProvider'")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Called by 'UserSettingsProvider'")]
        private static void VisualStudioEditorSettingsGui(string searchContext)
        {
            if (CodeEditor.CurrentEditor is VisualStudioEditor visualStudioEditor)
            {
                visualStudioEditor.OnGUI();
            }
        }
    }
}
