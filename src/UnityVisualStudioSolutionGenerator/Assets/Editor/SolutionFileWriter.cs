#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides methods for writing Visual Studio solution files (.sln or .slnx) to disk or as text.
    /// </summary>
    public static class SolutionFileWriter
    {
        /// <summary>
        ///     The Windows standard new line string (<c>\r\n</c>).
        /// </summary>
        internal const string WindowsNewLine = "\r\n";

        /// <summary>
        ///     Generates a Visual Studio solution file content as a string.
        /// </summary>
        /// <param name="solutionFile">The solution file information.</param>
        /// <param name="newProjects">All projects that should be included inside the solution.</param>
        /// <returns>The generated solution file content as a string.</returns>
        internal static string WriteToText(SolutionFile solutionFile, IReadOnlyList<ProjectFile> newProjects)
        {
            using var stringWriter = new StringWriter();
            var useXmlFormat = solutionFile.IsXmlSolution;
            WriteTo(useXmlFormat, solutionFile.SolutionDirectoryPath, newProjects, stringWriter);

            var result = stringWriter.ToString();
            if (useXmlFormat && !result.EndsWith(WindowsNewLine, StringComparison.Ordinal))
            {
                result += WindowsNewLine;
            }

            return result;
        }

        /// <summary>
        ///     Generates a Visual Studio solution file and write it to a file. The file is overwritten so, we first write it to a temp file so any exceptions
        ///     while generating the file don't lead to a incomplete file.
        /// </summary>
        /// <param name="solutionFile">The solution file information.</param>
        /// <param name="projectFiles">All projects that should be included inside the solution.</param>
        [SuppressMessage("Security", "CA5351", Justification = "Hash is only used for comparison.")]
        internal static void WriteToFileSafe(SolutionFile solutionFile, IReadOnlyList<ProjectFile> projectFiles)
        {
            // we don't write directly to prevent exceptions
            var tempSolutionFileName = $"{solutionFile.SolutionFilePath}.temp";
            WriteToFile(solutionFile.IsXmlSolution, tempSolutionFileName, solutionFile.SolutionDirectoryPath, projectFiles);

            if (File.Exists(solutionFile.SolutionFilePath))
            {
                // only write if the content has changed
                using var md5Algorithm = MD5.Create();
                var hashOfNew = ComputeFileHash(tempSolutionFileName, md5Algorithm);
                var hashOfOld = ComputeFileHash(solutionFile.SolutionFilePath, md5Algorithm);

                if (hashOfOld.SequenceEqual(hashOfNew))
                {
                    // nothing changed -> don't overwrite original file (don't trigger reload in Visual Studio)
                    File.Delete(tempSolutionFileName);
                    return;
                }

                File.Delete(solutionFile.SolutionFilePath);
            }

            File.Move(tempSolutionFileName, solutionFile.SolutionFilePath);
        }

        private static void WriteToFile(
            bool useXmlFormat,
            string solutionFileName,
            string solutionDirectoryPath,
            IReadOnlyList<ProjectFile> projectFiles)
        {
            using var solutionWriter = new StreamWriter(File.Create(solutionFileName), Encoding.UTF8);
            WriteTo(useXmlFormat, solutionDirectoryPath, projectFiles, solutionWriter);
        }

        private static void WriteTo(bool useXmlFormat, string solutionDirectoryPath, IReadOnlyList<ProjectFile> projectFiles, TextWriter writer)
        {
            if (useXmlFormat)
            {
                XmlSolutionFileWriter.WriteTo(writer, solutionDirectoryPath, projectFiles);
            }
            else
            {
                LegacySolutionFileWriter.WriteTo(writer, solutionDirectoryPath, projectFiles);
            }
        }

        private static byte[] ComputeFileHash(string tempSolutionFilePath, HashAlgorithm md5Algorithm)
        {
            using var newSolutionReader = File.OpenRead(tempSolutionFilePath);
            return md5Algorithm.ComputeHash(newSolutionReader);
        }
    }
}
