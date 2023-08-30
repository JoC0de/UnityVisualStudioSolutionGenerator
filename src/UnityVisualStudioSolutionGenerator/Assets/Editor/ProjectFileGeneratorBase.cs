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
    public abstract class ProjectFileGeneratorBase
    {
        private readonly string filePath;

        private string? assemblyDefinitionFilePath;

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

        public string SolutionDirectoryPath { get; }

        protected string ProjectName { get; }

        protected XElement ProjectElement { get; }

        protected XNamespace XmlNamespace { get; }

        protected string AssemblyDefinitionFilePath
        {
            get
            {
                assemblyDefinitionFilePath ??= ExtractAssemblyDefinitionFilePath();
                return assemblyDefinitionFilePath;
            }
        }

        public static string DetermineNewProjectFilePath(string projectFilePath)
        {
            var projectXml = new ProjectFileGeneratorSdkStyle(XDocument.Load(projectFilePath), projectFilePath);
            return projectXml.IsProjectFromPackage() ? projectFilePath : Path.ChangeExtension(projectXml.AssemblyDefinitionFilePath, "csproj");
        }

        public bool IsProjectFromPackage()
        {
            return AssemblyDefinitionFilePath.Contains(
                $"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}PackageCache{Path.DirectorySeparatorChar}");
        }

        public void WriteProjectFile()
        {
            var outputFilePath = Path.ChangeExtension(AssemblyDefinitionFilePath, ".csproj");
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

        protected abstract void WriteProjectFileInternal(XmlWriter writer, string outputFileDirectoryPath);

        private static bool MatchesOnePattern(string? value, List<string[]> patterns)
        {
            // always accept '*'
            if (patterns.Exists(pattern => pattern.Length == 2 && pattern[0].Length == 0 && pattern[1].Length == 0))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(value) && patterns.Exists(pattern => MatchesPattern(value!, pattern));
        }

        private static bool MatchesPattern(string value, string[] pattern)
        {
            if (pattern.Length == 0)
            {
                return true;
            }

            if (pattern.Length == 1)
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
            var lastPatternIndex = pattern.Length - 1;

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
                .Where(noneItemPath => noneItemPath?.EndsWith(".asmdef") == true)
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
