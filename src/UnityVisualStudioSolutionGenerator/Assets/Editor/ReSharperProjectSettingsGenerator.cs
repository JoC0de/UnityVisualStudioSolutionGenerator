#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using UnityVisualStudioSolutionGenerator.Configuration;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Helper to generate a ReSharper settings file (.csproj.DotSettings) for each .csproj file. It contains value so that the namespace of code is
    ///     expected to start at the project root and all sup directories should be included in the namespace.
    /// </summary>
    internal static class ReSharperProjectSettingsGenerator
    {
        /// <summary>
        ///     Generate a ReSharper settings file (.csproj.DotSettings) for a .csproj file <paramref name="projectFilePath" />. It contains value so that the
        ///     namespace of code is expected to start at the project root and all sup directories should be included in the namespace.
        /// </summary>
        /// <param name="projectFilePath">The absolute path of the .csproj file to generate a matching ReSharper settings file.</param>
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "We don't use the string to compare.")]
        internal static void WriteSettingsIfMissing(string projectFilePath)
        {
            if (!GeneratorSettings.GenerateReSharperProjectSettings)
            {
                return;
            }

            var settingsFilePath = $"{projectFilePath}.DotSettings";
            var projectDirectory = Path.GetDirectoryName(projectFilePath) ??
                                   throw new InvalidOperationException($"Failed to get directory name of path '{projectFilePath}'");
            if (!Path.IsPathFullyQualified(projectDirectory))
            {
                throw new InvalidOperationException($"'{projectDirectory}' is not absolute, got file path: '{projectFilePath}'.");
            }

            var projectSupDirectoriesEncoded = GetSupDirectoriesWithSourceCode(projectDirectory)
                .Select(
                    directory => Path.GetRelativePath(projectDirectory, directory)
                        .ToLowerInvariant()
                        .Replace('/', '\\')
                        .Replace("\\", "_005C", StringComparison.Ordinal)
                        .Replace(".", "_002E", StringComparison.Ordinal))
                .ToList();
            if (File.Exists(settingsFilePath))
            {
                var currentContent = File.ReadAllText(settingsFilePath);
                if (projectSupDirectoriesEncoded.TrueForAll(
                        relativePathEncoded => currentContent.Contains(relativePathEncoded, StringComparison.Ordinal)))
                {
                    return;
                }
            }

            using var writer = new StreamWriter(settingsFilePath, false, Encoding.UTF8);
            writer.WriteLine(
                "<wpf:ResourceDictionary xml:space=\"preserve\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:s=\"clr-namespace:System;assembly=mscorlib\" xmlns:ss=\"urn:shemas-jetbrains-com:settings-storage-xaml\" xmlns:wpf=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">");
            foreach (var relativePathEncoded in projectSupDirectoriesEncoded)
            {
                writer.WriteLine(
                    $"    <s:Boolean x:Key=\"/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/={relativePathEncoded}/@EntryIndexedValue\">False</s:Boolean>");
            }

            writer.WriteLine("</wpf:ResourceDictionary>");
            LogHelper.LogVerbose($"Generated ReSharper project settings file {Path.GetFileName(settingsFilePath)}");
        }

        private static IEnumerable<string> GetSupDirectoriesWithSourceCode(string directoryPath)
        {
            foreach (var subDirectory in Directory.EnumerateDirectories(directoryPath))
            {
                if (Directory.EnumerateFiles(subDirectory, "*.asmdef", SearchOption.TopDirectoryOnly).Any() ||
                    Directory.EnumerateFiles(subDirectory, "*.asmref", SearchOption.TopDirectoryOnly).Any() ||
                    !Directory.EnumerateFiles(subDirectory, "*.cs", SearchOption.AllDirectories).Any())
                {
                    // exclude sub-projects and only keep if any sub-directory has source code
                    continue;
                }

                yield return subDirectory;

                foreach (var subSubDirectory in GetSupDirectoriesWithSourceCode(subDirectory))
                {
                    yield return subSubDirectory;
                }
            }
        }
    }
}
