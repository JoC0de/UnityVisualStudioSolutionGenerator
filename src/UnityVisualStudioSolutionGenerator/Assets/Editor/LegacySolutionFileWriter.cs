#nullable enable

using System.Collections.Generic;
using System.IO;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Writes a Visual Studio solution file (.sln).
    /// </summary>
    public static class LegacySolutionFileWriter
    {
        /// <summary>
        ///     Writes the solution file using legacy (.sln) format to the specified writer.
        /// </summary>
        /// <param name="writer">The writer to write the solution file to.</param>
        /// <param name="solutionDirectoryPath">The absolute path of the directory containing the .sln file.</param>
        /// <param name="allProjects">All project files included in the solution.</param>
        internal static void WriteTo(TextWriter writer, string solutionDirectoryPath, IReadOnlyList<ProjectFile> allProjects)
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
    }
}
