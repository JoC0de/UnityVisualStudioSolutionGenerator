#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            AddSourceCodeWatcherForProjectInternal(projectRootDirectoryPath);
        }

        /// <summary>
        ///     Create all required <see cref="SourceCodeFileWatcher" />'s for the <paramref name="projectFiles" /> should be called after the domain is reloaded
        ///     and therefor all <see cref="SourceCodeFileWatcher" />'s are destroyed.
        /// </summary>
        /// <param name="projectFiles">All project files from the .sln.</param>
        public static void Initialize(IEnumerable<ProjectFile> projectFiles)
        {
            if (!FileWatcherFeatureEnabled())
            {
                DisableProjectSourceCodeWatchers();
                return;
            }

            foreach (var project in projectFiles)
            {
                if (!File.Exists(project.FilePath))
                {
                    continue;
                }

                string projectRootDirectoryPath;
                if (File.Exists(Path.ChangeExtension(project.FilePath, ".asmdef")))
                {
                    // fast path without needing to read the content of the .csproj because the .asmdef file is directly next to the .csproj file.
                    if (ProjectFileParser.IsProjectFileFromPackageCache(project.FilePath))
                    {
                        continue;
                    }

                    projectRootDirectoryPath = ProjectFileParser.GetProjectRootDirectoryPath(project.FilePath);
                }
                else
                {
                    var generator = ProjectFileGeneratorBase.Create(project.FilePath);

                    if (generator.IsProjectFromPackageCache())
                    {
                        continue;
                    }

                    projectRootDirectoryPath = generator.GetProjectRootDirectoryPath();
                }

                AddSourceCodeWatcherForProjectInternal(projectRootDirectoryPath);
            }
        }

        private static void AddSourceCodeWatcherForProjectInternal(string projectRootDirectoryPath)
        {
            var projectDirectoryPathWithSeparator = projectRootDirectoryPath + Path.DirectorySeparatorChar;
            if (ProjectSourceCodeWatchers.Keys.Any(
                    watchedDirectoryPath => projectDirectoryPathWithSeparator.StartsWith(watchedDirectoryPath, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            ProjectSourceCodeWatchers.Add(projectDirectoryPathWithSeparator, new SourceCodeFileWatcher(projectRootDirectoryPath));
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
