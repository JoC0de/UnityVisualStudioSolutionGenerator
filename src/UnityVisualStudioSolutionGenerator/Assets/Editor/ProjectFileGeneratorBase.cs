#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Represents a base class for generating C# project files.
    /// </summary>
    public abstract class ProjectFileGeneratorBase
    {
        private readonly string filePath;

        private string? assemblyDefinitionFilePath;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProjectFileGeneratorBase" /> class.
        /// </summary>
        /// <param name="document">The XML document containing the C# project file generated from Unity.</param>
        /// <param name="filePath">The absolute path of the project file.</param>
        protected ProjectFileGeneratorBase(XDocument document, string filePath)
        {
            _ = document ?? throw new ArgumentNullException(nameof(document));
            this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            if (document.Root is null)
            {
                throw new ArgumentException($"Input xml '{filePath}' has no 'Root' element.", nameof(document));
            }

            XmlNamespace = document.Root.Name.Namespace;
            ProjectElement = document.Element(XmlNamespace + "Project") ??
                             throw new ArgumentException($"Input xml '{filePath}' has no 'Project' element.", nameof(document));
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
            SolutionDirectoryPath = Path.GetFullPath(
                Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Failed to get directory path of '{filePath}'"));
        }

        /// <summary>
        ///     Gets the absolute path of the solution directory.
        /// </summary>
        public string SolutionDirectoryPath { get; }

        /// <summary>
        ///     Gets the name of the project, the name of the project file.
        /// </summary>
        protected string ProjectName { get; }

        /// <summary>
        ///     Gets the root project XML element.
        /// </summary>
        protected XElement ProjectElement { get; }

        /// <summary>
        ///     Gets the namespace of the project XML document.
        /// </summary>
        protected XNamespace XmlNamespace { get; }

        /// <summary>
        ///     Gets the path of the assembly definition file associated with this project. It is extracted from the project file generated by Unity.
        /// </summary>
        protected string AssemblyDefinitionFilePath
        {
            get
            {
                assemblyDefinitionFilePath ??= ExtractAssemblyDefinitionFilePath();
                return assemblyDefinitionFilePath;
            }
        }

        private string ProjectOutputFilePath => Path.ChangeExtension(AssemblyDefinitionFilePath, ".csproj");

        /// <summary>
        ///     Determines the path of the new project file based on the assembly definition file included in the project.
        /// </summary>
        /// <param name="projectFilePath">The absolute path of the project file.</param>
        /// <returns>The new project file path.</returns>
        public static string DetermineNewProjectFilePath(string projectFilePath)
        {
            var projectXml = new ProjectFileGeneratorSdkStyle(XDocument.Load(projectFilePath), projectFilePath);
            return projectXml.IsProjectFromPackage() ? projectFilePath : projectXml.ProjectOutputFilePath;
        }

        /// <summary>
        ///     Determines whether the project is from the package cache.
        /// </summary>
        /// <returns>True if the project file is from a package, False otherwise.</returns>
        public bool IsProjectFromPackage()
        {
            return AssemblyDefinitionFilePath.Contains(
                $"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}PackageCache{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Writes the project file to disk inside <see cref="ProjectOutputFilePath" />.
        /// </summary>
        public void WriteProjectFile()
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
            WriteProjectFileInternal(innerWriter, outputFileDirectoryPath);
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
        protected abstract void WriteProjectFileInternal(XmlWriter writer, string outputFileDirectoryPath);

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

        private string ExtractAssemblyDefinitionFilePath()
        {
            var assemblyDefinitionFilePaths = ProjectElement.Descendants(XmlNamespace + "None")
                .Select(noneElement => noneElement.Attribute("Include")?.Value)
                .Where(noneItemPath => noneItemPath?.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            if (assemblyDefinitionFilePaths.Count != 1)
            {
                throw new InvalidOperationException(
                    $"The csproj file '{filePath}' need to have exactly one '.asmdef' file but it has ['{string.Join("', '", assemblyDefinitionFilePaths)}']");
            }

            return Path.GetFullPath(assemblyDefinitionFilePaths[0], SolutionDirectoryPath);
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
