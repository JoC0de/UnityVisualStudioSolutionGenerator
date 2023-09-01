#nullable enable
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor;

namespace UnityVisualStudioSolutionGenerator
{
    public static class MenuItemProvider
    {
        [MenuItem("Visual Studio/Open Solution", priority = 0)]
        public static void OpenSolution()
        {
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }

        [MenuItem("Visual Studio/Generate Solution (Sdk-Style)", priority = 1)]
        public static void SyncSolutionSdkStyle()
        {
            GeneratorSettings.GenerateSdkStyleProjects = true;
            new VisualStudioEditor().SyncAll();
        }

        [MenuItem("Visual Studio/Generate Solution (Legacy-Style)", priority = 2)]
        public static void SyncSolution()
        {
            GeneratorSettings.GenerateSdkStyleProjects = false;
            new VisualStudioEditor().SyncAll();
        }

        [MenuItem("Visual Studio/Enable Verbose Logging", priority = 3)]
        public static void EnableVerboseLogging()
        {
            GeneratorSettings.LogVerbose = true;
        }

        [MenuItem("Visual Studio/Preferences", priority = 4)]
        public static void OpenPreferences()
        {
            SettingsService.OpenUserPreferences(GeneratorSettingsProvider.PreferencesPath);
        }
    }
}
