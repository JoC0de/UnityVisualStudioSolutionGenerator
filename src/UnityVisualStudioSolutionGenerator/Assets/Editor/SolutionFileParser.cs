#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Parses a visual studio solution file and gets the project files referenced from the solution.
    /// </summary>
    public static class SolutionFileParser
    {
        private const string ProjectStartTagName = "Project";

        private const string ProjectEndTagName = "EndProject";

        /// <summary>
        ///     Parses the solution file and gets the project files referenced from the solution.
        /// </summary>
        /// <param name="content">The solution file content.</param>
        /// <param name="solutionDirectoryPath">The absolute directory path of the solution file.</param>
        /// <param name="onlyIncludeUnityGeneratedProjects">
        ///     Indicating whether we should only include project files that are generated by Unity / files that are
        ///     directly inside the solution directory.
        /// </param>
        /// <returns>The referenced project files.</returns>
        public static (IReadOnlyList<ProjectFile> ProjectFiles, bool SourceContainsDuplicateProjects) Parse(
            string content,
            string solutionDirectoryPath,
            bool onlyIncludeUnityGeneratedProjects)
        {
            _ = content ?? throw new ArgumentNullException(nameof(content));
            var allProjects = new List<ProjectFile>();
            var projects = GetUsedProjectFiles(content, solutionDirectoryPath);
            var sourceContainsDuplicateProjects = false;
            foreach (var projectFile in projects)
            {
                // unity didn't remove the project-entry generated from us from the solution so we need to handle cases where we have duplicate entries
                // one that lies in the solution directory (the one generated by Unity / not changed by us)
                if (allProjects.Contains(projectFile))
                {
                    sourceContainsDuplicateProjects = true;
                    if (Path.GetDirectoryName(projectFile.FilePath.AsSpan()).Equals(solutionDirectoryPath, StringComparison.Ordinal))
                    {
                        allProjects.Remove(projectFile);
                        allProjects.Add(projectFile);
                    }
                }
                else if (!onlyIncludeUnityGeneratedProjects ||
                         Path.GetDirectoryName(projectFile.FilePath.AsSpan()).Equals(solutionDirectoryPath, StringComparison.Ordinal))
                {
                    allProjects.Add(projectFile);
                }
            }

            return (allProjects, sourceContainsDuplicateProjects);
        }

        /// <summary>
        ///     Parses the solution file and gets the project files referenced from the solution.
        /// </summary>
        /// <param name="solutionFile">The solution .</param>
        /// <param name="onlyIncludeUnityGeneratedProjects">
        ///     Indicating whether we should only include project files that are generated by Unity / files that are
        ///     directly inside the solution directory.
        /// </param>
        /// <returns>The referenced project files.</returns>
        internal static (IReadOnlyList<ProjectFile> ProjectFiles, bool SourceContainsDuplicateProjects) Parse(
            SolutionFile solutionFile,
            bool onlyIncludeUnityGeneratedProjects)
        {
            return Parse(File.ReadAllText(solutionFile.SolutionFilePath), solutionFile.SolutionDirectoryPath, onlyIncludeUnityGeneratedProjects);
        }

        private static IEnumerable<ProjectFile> GetUsedProjectFiles(string content, string solutionDirectoryPath)
        {
            if (!Directory.Exists(solutionDirectoryPath))
            {
                LogHelper.LogError($"Generating a solution file inside a directory that doesn't exists. Directory path: {solutionDirectoryPath}");
            }

            var projectIndex = 0;
            while (projectIndex < content.Length)
            {
                projectIndex = content.IndexOf(ProjectStartTagName, projectIndex, StringComparison.Ordinal);
                if (projectIndex < 0)
                {
                    break;
                }

                projectIndex += ProjectStartTagName.Length;
                projectIndex = SkipWhiteSpaces(content, projectIndex);

                if (content[projectIndex] != '(')
                {
                    // not a project start tag just the word Project
                    continue;
                }

                var endIndex = content.IndexOf(ProjectEndTagName, projectIndex, StringComparison.Ordinal);
                if (endIndex < 0)
                {
                    LogHelper.LogError($"Found 'Project' start but no 'EndProject' starting at char-index: {projectIndex}");
                    continue;
                }

                var firstCommaIndex = content.IndexOf(',', projectIndex, endIndex - projectIndex);
                if (firstCommaIndex < 0)
                {
                    LogHelper.LogError($"Found no ',' inside Project -> EndProject section: {content[projectIndex..endIndex]}");
                    continue;
                }

                ++firstCommaIndex; // skip the comma
                var secondCommaIndex = content.IndexOf(',', firstCommaIndex, endIndex - firstCommaIndex);
                if (secondCommaIndex < 0)
                {
                    LogHelper.LogError($"Found no second ',' inside Project -> EndProject section: {content[projectIndex..endIndex]}");
                    continue;
                }

                var projectFileNamePart = content[firstCommaIndex..secondCommaIndex].Trim('"', ' ');
                if (string.IsNullOrEmpty(projectFileNamePart))
                {
                    LogHelper.LogError($"Failed to extract csproj file name from Project -> EndProject section: {content[projectIndex..endIndex]}");
                    continue;
                }

                ++secondCommaIndex; // skip the comma
                var projectIdEndIndex = content.LastIndexOf('"', endIndex, endIndex - secondCommaIndex);
                if (projectIdEndIndex < 0)
                {
                    LogHelper.LogError(
                        $"Found no ProjectId ('\"') after the second ',' inside Project -> EndProject section: {content[projectIndex..endIndex]}");
                    continue;
                }

                var projectId = content[secondCommaIndex..projectIdEndIndex].Trim('"', ' ');
                projectIndex = endIndex + ProjectEndTagName.Length;

                var projectFilePath = Path.GetFullPath(projectFileNamePart, solutionDirectoryPath);
                yield return new ProjectFile(projectFilePath, projectId);
            }
        }

        private static int SkipWhiteSpaces(string content, int projectIndex)
        {
            while (projectIndex < content.Length && char.IsWhiteSpace(content[projectIndex]))
            {
                ++projectIndex;
            }

            return projectIndex;
        }
    }
}
