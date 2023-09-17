#nullable enable

using Unity.CodeEditor;
using UnityEditor;
using UnityVisualStudioSolutionGenerator.Configuration;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides menu items to access quick actions for Unity Visual Studio solutions.
    /// </summary>
    public static class MenuItemProvider
    {
        /// <summary>
        ///     Opens the C# project in Visual Studio.
        /// </summary>
        [MenuItem("Visual Studio/Open Solution", priority = 0)]
        public static void OpenSolution()
        {
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }

        /// <summary>
        ///     Returns whether or not the <see cref="OpenSolution" /> menu item should be enabled.
        /// </summary>
        /// <returns>True if the menu item should be enabled, False otherwise.</returns>
        [MenuItem("Visual Studio/Open Solution", true)]
        public static bool OpenSolutionEnabled()
        {
            return GeneratorSettings.IsVisualStudioEditorEnabled();
        }

        /// <summary>
        ///     Regenerates the Visual Studio solution file and the C# project files as SDK-style projects.
        /// </summary>
        [MenuItem("Visual Studio/Generate Solution (Sdk-Style)", priority = 1)]
        public static void SyncSolutionSdkStyle()
        {
            GeneratorSettings.GenerateSdkStyleProjects = true;
            CodeEditor.CurrentEditor.SyncAll();
        }

        /// <summary>
        ///     Returns whether or not the <see cref="SyncSolutionSdkStyle" /> menu item should be enabled.
        /// </summary>
        /// <returns>True if the menu item should be enabled, False otherwise.</returns>
        [MenuItem("Visual Studio/Generate Solution (Sdk-Style)", true)]
        public static bool SyncSolutionSdkStyleEnabled()
        {
            return GeneratorSettings.IsSolutionGeneratorEnabled();
        }

        /// <summary>
        ///     Regenerates the Visual Studio solution file and the C# project files as Legacy-style projects.
        /// </summary>
        [MenuItem("Visual Studio/Generate Solution (Legacy-Style)", priority = 2)]
        public static void SyncSolutionLegacyStyle()
        {
            GeneratorSettings.GenerateSdkStyleProjects = false;
            CodeEditor.CurrentEditor.SyncAll();
        }

        /// <summary>
        ///     Returns whether or not the <see cref="SyncSolutionLegacyStyle" /> menu item should be enabled.
        /// </summary>
        /// <returns>True if the menu item should be enabled, False otherwise.</returns>
        [MenuItem("Visual Studio/Generate Solution (Legacy-Style)", true)]
        public static bool SyncSolutionLegacyStyleEnabled()
        {
            return GeneratorSettings.IsSolutionGeneratorEnabled();
        }

        /// <summary>
        ///     Checks all '.cs' files to contain '#nullable enable' at the start.
        /// </summary>
        [MenuItem("Visual Studio/Apply enable nullable to all files", priority = 3)]
        public static void EnableNullableOnAllFiles()
        {
            SourceCodeFilesHandler.EnableNullableOnAllFiles(SolutionFile.CurrentProjectSolution);
        }

        /// <summary>
        ///     Returns whether or not the <see cref="EnableNullableOnAllFiles" /> menu item should be enabled.
        /// </summary>
        /// <returns>True if the menu item should be enabled, False otherwise.</returns>
        [MenuItem("Visual Studio/Apply enable nullable to all files", true)]
        public static bool EnableNullableOnAllFilesEnabled()
        {
            return GeneratorSettings.EnableNullableReferenceTypes;
        }

        /// <summary>
        ///     Opens the solution generation preferences page.
        /// </summary>
        [MenuItem("Visual Studio/Preferences", priority = 4)]
        public static void OpenPreferences()
        {
            SettingsService.OpenProjectSettings(GeneratorSettingsProvider.PreferencesPath);
        }
    }
}
