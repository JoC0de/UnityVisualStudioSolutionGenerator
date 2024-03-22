#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using UnityEditor;
using UnityEngine;
using UnityVisualStudioSolutionGenerator.Configuration;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     A post processor that triggers the generation of the updated Visual Studio Solution and Project files.
    ///     It is called by Unity after Unity generated a .csproj file or a .sln file.
    /// </summary>
    internal class VisualStudioAssetPostprocessor : AssetPostprocessor
    {
        private static readonly TimeSpan MinDelayBetweenGeneration = TimeSpan.FromSeconds(0.5);

        private static string? lastInputSolutionContent;

        private static string? lastOutputSolutionContent;

        private static DateTime lastSolutionGenerationTime;

        /// <summary>
        ///     Clears the cached solution content so the next time the solution is generated.
        /// </summary>
        public static void MarkAsChanged()
        {
            lastSolutionGenerationTime = DateTime.MinValue;
            lastInputSolutionContent = null;
            lastOutputSolutionContent = null;
        }

        /// <summary>
        ///     Called by unity if it reloads the assembly.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            var solutionFile = SolutionFile.CurrentProjectSolution;
            if (!GeneratorSettings.IsEnabled || !File.Exists(solutionFile.SolutionFilePath))
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var (allProjects, sourceContainsDuplicateProjects) = SolutionFileParser.Parse(solutionFile, false);
            const string lastUsedPlatformKey = "UnityVisualStudioSolutionGenerator.LastUsedPlatform";
            var lastUsedPlatform = (BuildTarget)SessionState.GetInt(lastUsedPlatformKey, (int)BuildTarget.NoTarget);
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!sourceContainsDuplicateProjects && lastUsedPlatform == activeBuildTarget)
            {
                // no need to regenerate the .csproj but
                // as we don't call 'GenerateNewProjects' we need to ensure that all SourceCodeFileWatcher's are running
                ProjectSourceCodeWatcherManager.Initialize(allProjects);
                return;
            }

            SessionState.SetInt(lastUsedPlatformKey, (int)activeBuildTarget);

            // Sometimes 'OnGeneratedCSProjectFiles' is not called when the reload order is wrong so we regenerate it here.
            // We detect this by checking if Unity generated the .sln and skipped all events like 'OnGeneratedCSProjectFiles'
            // so the .sln contains both the .csproj from Unity and the one generated by GenerateNewProjects.
            var newProjects = GenerateNewProjects(allProjects, solutionFile.SolutionDirectoryPath);

            SolutionFileWriter.WriteToFileSafe(solutionFile.SolutionFilePath, solutionFile.SolutionDirectoryPath, newProjects);
            lastSolutionGenerationTime = DateTime.UtcNow;
            LogHelper.LogInformation(
                $"Generated Visual Studio Solution in '{nameof(Initialize)}': '{solutionFile}' in {stopwatch.ElapsedMilliseconds} ms.");
        }

        /// <summary>
        ///     Called once all .csproj files and the .sln is generated.
        /// </summary>
        /// <remarks>
        ///     Code that calls this: see
        ///     <see href="https://github.com/needle-mirror/com.unity.ide.visualstudio/blob/master/Editor/ProjectGeneration/ProjectGeneration.cs" />.
        /// </remarks>
        private static void OnGeneratedCSProjectFiles()
        {
            var currentTime = DateTime.UtcNow;
            if (!GeneratorSettings.IsEnabled || currentTime - lastSolutionGenerationTime < MinDelayBetweenGeneration)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var solutionFile = SolutionFile.CurrentProjectSolution;
            var (allProjects, _) = SolutionFileParser.Parse(solutionFile, false);

            var newProjects = GenerateNewProjects(allProjects, solutionFile.SolutionDirectoryPath);

            SolutionFileWriter.WriteToFileSafe(solutionFile.SolutionFilePath, solutionFile.SolutionDirectoryPath, newProjects);
            lastSolutionGenerationTime = currentTime;
            LogHelper.LogInformation($"Generated Visual Studio Solution: '{solutionFile}' in {stopwatch.ElapsedMilliseconds} ms.");
        }

        /// <summary>
        ///     Called after Unity generated a .sln file.
        ///     We need to generate the .sln content here because else the unmodified file is written to the disk -> Visual Studio thinks it need to reload it.
        ///     Also <see cref="OnGeneratedCSProjectFiles" /> is not called for each change.
        /// </summary>
        /// <param name="path">The target path of the generated .sln file.</param>
        /// <param name="content">The content of the .sln file generated by Unity.</param>
        /// <returns>The probably changed content of the .sln file.</returns>
        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "We keep the input so we have anything.")]
        private static string OnGeneratedSlnSolution(string path, string content)
        {
            try
            {
                if (!GeneratorSettings.IsEnabled)
                {
                    return RemoveGeneratedProjectsFromSolution(path, content);
                }

                if (content.Equals(lastInputSolutionContent, StringComparison.Ordinal))
                {
                    lastSolutionGenerationTime = DateTime.UtcNow;
                    return lastOutputSolutionContent ??
                           throw new InvalidOperationException(
                               $"{nameof(lastOutputSolutionContent)} is null bug: {nameof(lastInputSolutionContent)} has a value.");
                }

                var stopwatch = Stopwatch.StartNew();
                var solutionDirectoryPath = GetDirectoryPath(path);
                var (allProjects, _) = SolutionFileParser.Parse(content, solutionDirectoryPath, false);
                if (!allProjects.All(project => File.Exists(project.FilePath)))
                {
                    return content;
                }

                var newProjects = GenerateNewProjects(allProjects, solutionDirectoryPath);
                var newContent = SolutionFileWriter.WriteToText(solutionDirectoryPath, newProjects);

                lastSolutionGenerationTime = DateTime.UtcNow;
                lastInputSolutionContent = content;
                lastOutputSolutionContent = newContent;
                LogHelper.LogInformation(
                    $"Generated content of Visual Studio Solution '{Path.GetFileName(path)}' in {stopwatch.ElapsedMilliseconds} ms.");
                return newContent;
            }
            catch (Exception e)
            {
                LogHelper.LogWarning(
                    $"Failed to generate solution on '{nameof(OnGeneratedSlnSolution)}' event. Retrying in '{nameof(OnGeneratedCSProjectFiles)}'. Error:\n{e}");
            }

            return content;
        }

        private static List<ProjectFile> GenerateNewProjects(IReadOnlyList<ProjectFile> allProjects, string solutionDirectoryPath)
        {
            var newProjects = new List<ProjectFile>();
            foreach (var project in allProjects)
            {
                var projectFilePath = project.FilePath;
                var projectFilePathInSolutionDirectory = Path.Combine(solutionDirectoryPath, Path.GetFileName(projectFilePath));
                if (projectFilePathInSolutionDirectory != projectFilePath && File.Exists(projectFilePathInSolutionDirectory))
                {
                    // prefer file from solution directory (the one generated by Unity), if it exists.
                    projectFilePath = projectFilePathInSolutionDirectory;
                }

                if (!File.Exists(projectFilePath))
                {
                    LogHelper.LogInformation($"The project file '{projectFilePath}' doesn't exists so we skip it from the solution.");
                    continue;
                }

                var generator = ProjectFileGeneratorBase.Create(projectFilePath);

                if (generator.IsProjectFromPackageCache())
                {
                    LogHelper.LogVerbose(
                        $"The project '{Path.GetFileNameWithoutExtension(projectFilePath)}' is a Unity Package so we don't change the '.csproj' file.");
                    newProjects.Add(project);
                    continue;
                }

                if (!File.Exists(generator.AssemblyDefinitionFilePath))
                {
                    LogHelper.LogInformation(
                        $"The '.asmdef' file '{generator.AssemblyDefinitionFilePath}' doesn't exists so we exclude the project from the solution.");
                    continue;
                }

                var newProjectFilePath = generator.WriteProjectFile(solutionDirectoryPath);

                ReSharperProjectSettingsGenerator.WriteSettingsIfMissing(newProjectFilePath);
                ProjectSourceCodeWatcherManager.AddSourceCodeWatcherForProject(GetDirectoryPath(newProjectFilePath));
                newProjects.Add(new ProjectFile(newProjectFilePath, project.Id));
            }

            foreach (var additionalIncludedSolution in GeneratorSettings.AdditionalIncludedSolutions)
            {
                if (!File.Exists(additionalIncludedSolution))
                {
                    LogHelper.LogInformation(
                        $"The additional solution file '{additionalIncludedSolution}' doesn't exists so we skip it from the solution.");
                    continue;
                }

                var (additionalProjects, _) = SolutionFileParser.Parse(
                    File.ReadAllText(additionalIncludedSolution),
                    GetDirectoryPath(additionalIncludedSolution),
                    false);
                foreach (var additionalProject in additionalProjects)
                {
                    if (newProjects.Contains(additionalProject))
                    {
                        continue;
                    }

                    if (!File.Exists(additionalProject.FilePath))
                    {
                        // additional projects need to exist on disk as they are not generated by this unity instance
                        continue;
                    }

                    ProjectSourceCodeWatcherManager.AddSourceCodeWatcherForProject(GetDirectoryPath(additionalProject.FilePath));
                    newProjects.Add(additionalProject);
                }
            }

            foreach (var additionalProjectFile in GeneratorSettings.AdditionalIncludedProjectFiles)
            {
                if (!File.Exists(additionalProjectFile))
                {
                    LogHelper.LogInformation(
                        $"The additional project file '{additionalProjectFile}' doesn't exists so we skip it from the solution.");
                    continue;
                }

                using var hashAlgorithm = SHA256.Create();
                var projectId = new Guid(hashAlgorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(additionalProjectFile)).AsSpan(0, 16)).ToString("d").ToUpperInvariant();
                var additionalProject = new ProjectFile(additionalProjectFile, projectId);
                ProjectSourceCodeWatcherManager.AddSourceCodeWatcherForProject(GetDirectoryPath(additionalProject.FilePath));
                newProjects.Add(additionalProject);
            }

            return newProjects;
        }

        private static string RemoveGeneratedProjectsFromSolution(string path, string content)
        {
            // if one of the .csproj files are inside a folder they are generated from this plugin so we need to remove them
            if (content.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) == -1)
            {
                return content;
            }

            var solutionDirectoryPath = GetDirectoryPath(path);
            var (allProjects, _) = SolutionFileParser.Parse(content, solutionDirectoryPath, !GeneratorSettings.IsEnabled);
            return SolutionFileWriter.WriteToText(solutionDirectoryPath, allProjects);
        }

        private static string GetDirectoryPath(string path)
        {
            return Path.GetFullPath(Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Failed to get directory path of '{path}'"));
        }
    }
}
