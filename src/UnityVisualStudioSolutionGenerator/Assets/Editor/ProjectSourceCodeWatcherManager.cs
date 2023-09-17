#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityVisualStudioSolutionGenerator.Configuration;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Static manager of <see cref="SourceCodeFileWatcher" /> keeps them running even if Unity reloads the AppDomain (deletes static instances).
    /// </summary>
    internal static class ProjectSourceCodeWatcherManager
    {
        private static readonly Dictionary<string, SourceCodeFileWatcher> ProjectSourceCodeWatchers = new();

        /// <summary>
        ///     Creates a <see cref="SourceCodeFileWatcher" /> that watches all source code file changes inside the project.
        /// </summary>
        /// <param name="projectRootDirectoryPath">
        ///     The root directory of all project files. Is used as a starting point for the watcher, so only files inside
        ///     this folder or any sup-folder are watched.
        /// </param>
        public static void AddSourceCodeWatcherForProject(string projectRootDirectoryPath)
        {
            if (!FileWatcherFeatureEnabled())
            {
                DisableProjectSourceCodeWatchers();
                return;
            }

            var projectDirectoryPathWithSeparator = projectRootDirectoryPath + Path.DirectorySeparatorChar;
            if (ProjectSourceCodeWatchers.Keys.Any(
                    watchedDirectoryPath => projectDirectoryPathWithSeparator.StartsWith(watchedDirectoryPath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            ProjectSourceCodeWatchers.Add(projectDirectoryPathWithSeparator, new SourceCodeFileWatcher(projectRootDirectoryPath));
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (!FileWatcherFeatureEnabled())
            {
                DisableProjectSourceCodeWatchers();
                return;
            }

            var solutionFile = SolutionFile.CurrentProjectSolution;
            if (!File.Exists(solutionFile.SolutionFilePath))
            {
                LogHelper.LogVerbose(
                    $"Skipping {nameof(ProjectSourceCodeWatcherManager)}.{nameof(Initialize)} because '{Path.GetFileName(solutionFile.SolutionFilePath)}' doesn't exist.");
                return;
            }

            var allProjects = SolutionFileParser.Parse(solutionFile, false);
            foreach (var project in allProjects)
            {
                var generator = ProjectFileGeneratorBase.Create(project.FilePath);

                if (generator.IsProjectFromPackageCache())
                {
                    continue;
                }

                var projectRootDirectoryPath = generator.GetProjectRootDirectoryPath();

                AddSourceCodeWatcherForProject(projectRootDirectoryPath);
            }
        }

        private static bool FileWatcherFeatureEnabled()
        {
            return GeneratorSettings.IsSolutionGeneratorEnabled() &&
                   (GeneratorSettings.TrackMetaDeletion || GeneratorSettings.EnableNullableReferenceTypes);
        }

        private static void DisableProjectSourceCodeWatchers()
        {
            foreach (var sourceCodeFileWatcher in ProjectSourceCodeWatchers.Values)
            {
                sourceCodeFileWatcher.Dispose();
            }

            ProjectSourceCodeWatchers.Clear();
        }
    }
}
