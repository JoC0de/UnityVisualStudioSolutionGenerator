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
    ///     Writes a Visual Studio solution file.
    /// </summary>
    public static class SolutionFileWriter
    {
        /// <summary>
        ///     Generates a Visual Studio solution file and write it to a string.
        /// </summary>
        /// <param name="solutionDirectoryPath">The absolute path of the solution directory.</param>
        /// <param name="allProjects">All projects that should be included inside the solution.</param>
        /// <returns>The content of the generated solution as plain text.</returns>
        public static string WriteToText(string solutionDirectoryPath, IReadOnlyList<ProjectFile> allProjects)
        {
            _ = allProjects ?? throw new ArgumentNullException(nameof(allProjects));
            using var writer = new StringWriter();
            GenerateVisualStudioSolution(writer, solutionDirectoryPath, allProjects);
            return writer.ToString();
        }

        /// <summary>
        ///     Generates a Visual Studio solution file and write it to a file. The file is overwritten so, we first write it to a temp file so any exceptions
        ///     while generating the file don't lead to a incomplete file.
        /// </summary>
        /// <param name="outputFilePath">The file path to write the generated solution file.</param>
        /// <param name="solutionDirectoryPath">The absolute path of the solution directory.</param>
        /// <param name="projectFiles">All projects that should be included inside the solution.</param>
        [SuppressMessage("Security", "CA5351", Justification = "Hash is only used for comparison.")]
        public static void WriteToFileSafe(string outputFilePath, string solutionDirectoryPath, IReadOnlyList<ProjectFile> projectFiles)
        {
            // we don't write directly to prevent exceptions
            var tempSolutionFilePath = $"{outputFilePath}.temp";
            WriteToFile(tempSolutionFilePath, solutionDirectoryPath, projectFiles);

            if (File.Exists(outputFilePath))
            {
                // only write if the content has changed
                using var md5Algorithm = MD5.Create();
                var hashOfNew = ComputeFileHash(tempSolutionFilePath, md5Algorithm);
                var hashOfOld = ComputeFileHash(outputFilePath, md5Algorithm);

                if (hashOfOld.SequenceEqual(hashOfNew))
                {
                    // nothing changed -> don't overwrite original file (don't trigger reload in Visual Studio)
                    File.Delete(tempSolutionFilePath);
                    return;
                }

                File.Delete(outputFilePath);
            }

            File.Move(tempSolutionFilePath, outputFilePath);
        }

        private static void GenerateVisualStudioSolution(TextWriter writer, string solutionDirectoryPath, IReadOnlyList<ProjectFile> allProjects)
        {
            writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            writer.WriteLine("# Visual Studio Version 17");
            writer.WriteLine("VisualStudioVersion = 17.0.32014.148");
            writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");

            foreach (var project in allProjects)
            {
                var projectName = project.ProjectName;
                var relativeProjectFilePath = Path.GetRelativePath(solutionDirectoryPath, project.FilePath);
                writer.WriteLine(
                    "Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{0}\", \"{1}\", \"{2}\"",
                    projectName,
                    relativeProjectFilePath,
                    project.Id);
                writer.WriteLine("EndProject");
            }

            writer.WriteLine("Global");
            writer.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            writer.WriteLine("\t\tDebug|Any CPU = Debug|Any CPU");
            writer.WriteLine("\t\tRelease|Any CPU = Release|Any CPU");
            writer.WriteLine("\tEndGlobalSection");
            writer.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var project in allProjects)
            {
                writer.WriteLine("\t\t{0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", project.Id);
                writer.WriteLine("\t\t{0}.Debug|Any CPU.Build.0 = Debug|Any CPU", project.Id);
                writer.WriteLine("\t\t{0}.Release|Any CPU.ActiveCfg = Release|Any CPU", project.Id);
                writer.WriteLine("\t\t{0}.Release|Any CPU.Build.0 = Release|Any CPU", project.Id);
            }

            writer.WriteLine("\tEndGlobalSection");
            writer.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
            writer.WriteLine("\t\tHideSolutionNode = FALSE");
            writer.WriteLine("\tEndGlobalSection");
            writer.WriteLine("EndGlobal");
        }

        private static void WriteToFile(string outputFilePath, string solutionDirectoryPath, IReadOnlyList<ProjectFile> projectFiles)
        {
            using var solutionWriter = new StreamWriter(File.Create(outputFilePath), Encoding.UTF8);
            GenerateVisualStudioSolution(solutionWriter, solutionDirectoryPath, projectFiles);
        }

        private static byte[] ComputeFileHash(string tempSolutionFilePath, HashAlgorithm md5Algorithm)
        {
            using var newSolutionReader = File.OpenRead(tempSolutionFilePath);
            return md5Algorithm.ComputeHash(newSolutionReader);
        }
    }
}
