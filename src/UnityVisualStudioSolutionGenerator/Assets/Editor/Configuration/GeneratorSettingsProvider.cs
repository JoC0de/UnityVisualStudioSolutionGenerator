using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides the settings of this package to the Unity preferences window.
    /// </summary>
    internal static class GeneratorSettingsProvider
    {
        /// <summary>
        ///     The path to the settings of this package in the Unity preferences window.
        /// </summary>
        internal const string PreferencesPath = "Preferences/Visual Studio Solution Generator";

        /// <summary>
        ///     Creates the settings provider for this package.
        ///     This is called by the Unity preferences window.
        /// </summary>
        /// <returns>The settings provider / the class used to generate the UI to change the settings.</returns>
        [SettingsProvider]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Unity")]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new UserSettingsProvider(PreferencesPath, GeneratorSettingsManager.Instance, new[] { typeof(GeneratorSettingsProvider).Assembly });
        }
    }
}
