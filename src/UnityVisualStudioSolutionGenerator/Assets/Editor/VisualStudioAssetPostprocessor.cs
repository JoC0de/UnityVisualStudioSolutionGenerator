#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    internal class VisualStudioAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        ///     Called once all .csproj files and the .sln is generated.
        /// </summary>
        /// <remarks>
        ///     Code that calls this: see
        ///     <see href="https://github.com/needle-mirror/com.unity.ide.visualstudio/blob/master/Editor/ProjectGeneration/ProjectGeneration.cs" />.
        /// </remarks>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by the 'Unity'")]
        private static void OnGeneratedCSProjectFiles()
        {
            if (!GeneratorSettings.IsEnabled)
            {
                return;
            }

            LogHelper.LogInformation(
                $"Generated Visual Studio Solution: {Path.GetFileName(Path.GetFullPath(Path.Combine(Application.dataPath, "..")))}.sln.");
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "The project should fallback if failed.")]
        private static string OnGeneratedSlnSolution(string path, string content)
        {
            if (!GeneratorSettings.IsEnabled)
            {
                return RemoveGeneratedProjectsFromSolution(path, content);
            }

            try
            {
                var solutionDirectoryPath = Path.GetFullPath(
                    Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Failed to get directory path of '{path}'"));

                var newProjects = DetermineNewSolutionProjects(content, solutionDirectoryPath);
                return newProjects is null ? content : SolutionFileWriter.WriteToText(solutionDirectoryPath, newProjects);
            }
            catch (Exception exception)
            {
                LogHelper.LogError($"Generating Visual Studio Solution file ({Path.GetFileName(path)}) failed with: {exception}");
            }

            return content;
        }

        private static string RemoveGeneratedProjectsFromSolution(string path, string content)
        {
            // if one of the .csproj files are inside a folder they are generated from this plugin so we need to remove them
            if (content.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) == -1)
            {
                return content;
            }

            var solutionDirectoryPath = Path.GetFullPath(
                Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Failed to get directory path of '{path}'"));
            var allProjects = SolutionFileParser.Parse(content, solutionDirectoryPath);
            return SolutionFileWriter.WriteToText(solutionDirectoryPath, allProjects);
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "The solution should fallback if failed.")]
        private static string OnGeneratedCSProject(string path, string content)
        {
            if (!GeneratorSettings.IsEnabled)
            {
                return content;
            }

            try
            {
                var document = XDocument.Parse(content);
                ProjectFileGeneratorBase generator = GeneratorSettings.GenerateSdkStyleProjects
                    ? new ProjectFileGeneratorSdkStyle(document, path)
                    : new ProjectFileGeneratorLegacyStyle(document, path);

                if (generator.IsProjectFromPackage())
                {
                    LogHelper.LogVerbose(
                        $"The project '{Path.GetFileNameWithoutExtension(path)}' is a Unity Package so we don't change the '.csproj' file.");
                    return content;
                }

                generator.WriteProjectFile();

                GenerateSolution(generator.SolutionDirectoryPath);
            }
            catch (Exception exception)
            {
                LogHelper.LogError($"Generating Visual Studio Project file ({Path.GetFileName(path)}) failed with: {exception}");
            }

            return content;
        }

        private static void GenerateSolution(string solutionDirectoryPath)
        {
            var solutionFilePath = Path.Combine(solutionDirectoryPath, $"{Path.GetFileName(solutionDirectoryPath)}.sln");
            var newProjects = DetermineNewSolutionProjects(File.ReadAllText(solutionFilePath), solutionDirectoryPath);
            if (newProjects is null)
            {
                return;
            }

            SolutionFileWriter.WriteToFileSafe(solutionFilePath, solutionDirectoryPath, newProjects);
        }

        private static List<ProjectFile>? DetermineNewSolutionProjects(string currentSolutionContent, string solutionDirectoryPath)
        {
            var allProjects = SolutionFileParser.Parse(currentSolutionContent, solutionDirectoryPath);

            var canGenerateSolutionFile = allProjects.All(project => File.Exists(project.FilePath));
            return canGenerateSolutionFile
                ? allProjects.Select(project => new ProjectFile(ProjectFileGeneratorBase.DetermineNewProjectFilePath(project.FilePath), project.Id))
                    .ToList()
                : null;
        }
    }
}
