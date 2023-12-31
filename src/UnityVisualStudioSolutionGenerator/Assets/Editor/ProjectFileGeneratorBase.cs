#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityVisualStudioSolutionGenerator.Configuration;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Represents a base class for generating C# project files.
    /// </summary>
    public abstract class ProjectFileGeneratorBase : ProjectFileParser
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProjectFileGeneratorBase" /> class.
        /// </summary>
        /// <param name="filePath">The absolute path of the project file.</param>
        protected ProjectFileGeneratorBase(string filePath)
            : base(filePath)
        {
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
        }

        /// <summary>
        ///     Gets the name of the project, the name of the project file.
        /// </summary>
        protected string ProjectName { get; }

        private string ProjectOutputFilePath => Path.ChangeExtension(AssemblyDefinitionFilePath, ".csproj");

        /// <summary>
        ///     Writes the project file to disk inside <see cref="ProjectOutputFilePath" />.
        /// </summary>
        /// <param name="solutionDirectoryPath">The absolute path of th directory containing the .sln file.</param>
        /// <returns>The absolute path to witch the file was written.</returns>
        public string WriteProjectFile(string solutionDirectoryPath)
        {
            var outputFilePath = ProjectOutputFilePath;
            var outputFileDirectoryPath = Path.GetDirectoryName(outputFilePath) ??
                                          throw new InvalidOperationException($"Failed to get directory path of '{outputFilePath}'");

            RemoveExcludedAnalyzers();

            using var outputStream = new StreamWriter(outputFilePath, false, Encoding.UTF8);
            using var innerWriter = XmlWriter.Create(
                outputStream,
                new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    NamespaceHandling = NamespaceHandling.OmitDuplicates,
                    OmitXmlDeclaration = GeneratorSettings.GenerateSdkStyleProjects,
                    Indent = true,
                    IndentChars = "    ",
                });
            WriteProjectFileInternal(innerWriter, outputFileDirectoryPath, solutionDirectoryPath);

            return outputFilePath;
        }

        /// <summary>
        ///     Creates a new instance of a project file generator, based on the <see cref="GeneratorSettings.GenerateSdkStyleProjects" /> setting.
        /// </summary>
        /// <param name="filePath">The path to the .csproj file tho read the information for the new project file content from.</param>
        /// <returns>The new instance.</returns>
        internal static ProjectFileGeneratorBase Create(string filePath)
        {
            return GeneratorSettings.GenerateSdkStyleProjects
                ? new ProjectFileGeneratorSdkStyle(filePath)
                : new ProjectFileGeneratorLegacyStyle(filePath);
        }

        /// <summary>
        ///     Determines the root directory that contains all project files, the directory that contains the
        ///     <see cref="ProjectFileParser.AssemblyDefinitionFilePath" />.
        /// </summary>
        /// <returns>The absolute path to the project root directory.</returns>
        internal string GetProjectRootDirectoryPath()
        {
            return GetProjectRootDirectoryPath(AssemblyDefinitionFilePath);
        }

        /// <summary>
        ///     Determines the path of the new project file based on the assembly definition file included in the project.
        /// </summary>
        /// <param name="projectFilePath">The absolute path of the project file.</param>
        /// <returns>The new project file path.</returns>
        protected static string DetermineNewProjectFilePath(string projectFilePath)
        {
            var project = new ProjectFileGeneratorSdkStyle(projectFilePath);
            return project.IsProjectFromPackageCache() ? projectFilePath : project.ProjectOutputFilePath;
        }

        /// <summary>
        ///     Finds all projects that are inside a sub-folder inside the project directory.
        /// </summary>
        /// <param name="outputFileDirectoryPath">The path of the project directory witch is searched for sub projects.</param>
        /// <returns>A list of all path's of directories containing sub-projects.</returns>
        protected static IEnumerable<string> FindSubProjectFolders(string outputFileDirectoryPath)
        {
            var foldersToIgnore = Directory
                .EnumerateFiles(
                    outputFileDirectoryPath,
                    "*.asmdef",
                    new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true })
                .Select(
                    assemblyDefinitionFilePath => Path.GetRelativePath(outputFileDirectoryPath, Path.GetDirectoryName(assemblyDefinitionFilePath)))
                .Where(relativeSubProjectDirectory => !string.IsNullOrEmpty(relativeSubProjectDirectory) && relativeSubProjectDirectory != ".");
            return foldersToIgnore;
        }

        /// <summary>
        ///     Writes the project file to a XML writer.
        /// </summary>
        /// <param name="writer">The XML writer.</param>
        /// <param name="outputFileDirectoryPath">The absolute path of the output folder.</param>
        /// <param name="solutionDirectoryPath">The absolute path of th directory containing the .sln file.</param>
        protected abstract void WriteProjectFileInternal(XmlWriter writer, string outputFileDirectoryPath, string solutionDirectoryPath);

        private static bool MatchesOnePattern(string? value, List<string[]> patterns)
        {
            // always accept '*' (* is split into two empty strings)
            if (patterns.Exists(pattern => pattern.Length == 2 && pattern[0].Length == 0 && pattern[1].Length == 0))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(value) && patterns.Exists(pattern => MatchesPattern(value!, pattern));
        }

        private static bool MatchesPattern(string value, IReadOnlyList<string> pattern)
        {
            if (pattern.Count == 0)
            {
                return true;
            }

            if (pattern.Count == 1)
            {
                // no '*'
                return string.Equals(value, pattern[0], StringComparison.OrdinalIgnoreCase);
            }

            // first pattern is a start with condition
            if (pattern[0].Length != 0 && !value.StartsWith(pattern[0], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var matchStartIndex = pattern[0].Length;
            var lastPatternIndex = pattern.Count - 1;

            // last pattern is a end with condition
            if (pattern[lastPatternIndex].Length != 0 &&
                !value.AsSpan()[matchStartIndex..].EndsWith(pattern[lastPatternIndex], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var matchEndIndex = value.Length - pattern[lastPatternIndex].Length;
            for (var patternIndex = 1; patternIndex < lastPatternIndex; patternIndex++)
            {
                if (matchStartIndex == matchEndIndex)
                {
                    return false;
                }

                matchStartIndex = value.IndexOf(
                    pattern[patternIndex],
                    matchStartIndex,
                    matchEndIndex - matchStartIndex,
                    StringComparison.OrdinalIgnoreCase);
                if (matchStartIndex == -1)
                {
                    return false;
                }

                matchStartIndex += pattern[patternIndex].Length;
            }

            return true;
        }

        private void RemoveExcludedAnalyzers()
        {
            var excludedAnalyzers = GeneratorSettings.ExcludedAnalyzers;
            if (excludedAnalyzers.Count == 0)
            {
                return;
            }

            var patterns = excludedAnalyzers.Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                .Select(pattern => pattern.Trim().Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Split('*'))
                .ToList();
            ProjectElement.Descendants(XmlNamespace + "Analyzer")
                .Where(analyzerElement => MatchesOnePattern(analyzerElement.Attribute("Include")?.Value, patterns))
                .Remove();
        }
    }
}
