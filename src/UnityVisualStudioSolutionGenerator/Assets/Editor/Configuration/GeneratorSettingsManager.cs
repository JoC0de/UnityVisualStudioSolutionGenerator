#nullable enable

using System;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator.Configuration
{
    /// <summary>
    ///     Stores the singleton instance of the settings of this package.
    /// </summary>
    internal static class GeneratorSettingsManager
    {
        private const string PackageName = "com.github-joc0de.visual-studio-solution-generator";

        private static readonly Lazy<Settings> LazyInstance = new(() => new Settings(PackageName));

        /// <summary>
        ///     Gets the singleton instance of the settings of this package.
        /// </summary>
        public static Settings Instance => LazyInstance.Value;

        /// <summary>
        ///     Saves the settings to the file.
        /// </summary>
        public static void Save()
        {
            Instance.Save();
        }
    }
}
