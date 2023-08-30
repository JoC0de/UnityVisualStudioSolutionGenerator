using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    public static class GeneratorSettingsProvider
    {
        internal const string PreferencesPath = "Preferences/Visual Studio Solution Generator";

        [SettingsProvider]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Unity")]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new UserSettingsProvider(PreferencesPath, GeneratorSettingsManager.Instance, new[] { typeof(GeneratorSettingsProvider).Assembly });
        }
    }
}
