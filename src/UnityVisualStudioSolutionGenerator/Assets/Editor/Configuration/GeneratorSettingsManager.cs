#nullable enable

using System;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    public static class GeneratorSettingsManager
    {
        private const string PackageName = "com.github-joc0de.visual-studio-solution-generator";

        private static readonly Lazy<Settings> LazyInstance = new(() => new Settings(PackageName));

        public static Settings Instance => LazyInstance.Value;

        public static void Save()
        {
            Instance.Save();
        }
    }
}
