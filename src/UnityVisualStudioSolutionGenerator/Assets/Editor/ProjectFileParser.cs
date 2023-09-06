#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides some generic functionality for parsing '.csproj' XML files.
    /// </summary>
    public class ProjectFileParser
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProjectFileParser" /> class.
        /// </summary>
        /// <param name="filePath">The absolute path of the project file.</param>
        public ProjectFileParser(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var document = XDocument.Load(filePath);
            if (document.Root is null)
            {
                throw new ArgumentException($"Input xml '{filePath}' has no 'Root' element.", nameof(filePath));
            }

            XmlNamespace = document.Root.Name.Namespace;
            ProjectElement = document.Element(XmlNamespace + "Project") ??
                             throw new ArgumentException($"Input xml '{filePath}' has no 'Project' element.", nameof(filePath));
            DirectoryPath = Path.GetFullPath(
                Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Failed to get directory path of '{filePath}'"));
        }

        /// <summary>
        ///     Gets the absolute path of the project file (.csproj).
        /// </summary>
        protected string FilePath { get; }

        /// <summary>
        ///     Gets the root project XML element.
        /// </summary>
        protected XElement ProjectElement { get; }

        /// <summary>
        ///     Gets the namespace of the project XML document.
        /// </summary>
        protected XNamespace XmlNamespace { get; }

        /// <summary>
        ///     Gets the absolute path of the directory containing this file.
        /// </summary>
        protected string DirectoryPath { get; }

        /// <summary>
        ///     Read the Project file and extract all source code files. But exclude files that are inside a 'PackageCache'.
        /// </summary>
        /// <returns>The absolute file path of all source code files, referenced by this project file.</returns>
        public IEnumerable<string> GetAllNonPackageSourceCodeFiles()
        {
            var sourceCodeFiles = ProjectElement.Descendants(XmlNamespace + "Compile")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(itemPath => itemPath is not null);

            var isFirstFile = true;
            foreach (var sourceCodeFile in sourceCodeFiles)
            {
                var absoluteFilePath = Path.GetFullPath(sourceCodeFile, DirectoryPath);
                if (isFirstFile && IsProjectFileFromPackageCache(absoluteFilePath))
                {
                    yield break;
                }

                isFirstFile = false;
                yield return absoluteFilePath;
            }
        }

        /// <summary>
        ///     Determines whether the project is from the package cache.
        /// </summary>
        /// <param name="projectFilePath">The absolute path to one project file (can also be a file that lies inside the project folder).</param>
        /// <returns>True if the project file is from a package that is located inside the 'PackageCache', False otherwise.</returns>
        protected static bool IsProjectFileFromPackageCache(string projectFilePath)
        {
            _ = projectFilePath ?? throw new ArgumentNullException(nameof(projectFilePath));

            return projectFilePath.Contains(
                $"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}PackageCache{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}