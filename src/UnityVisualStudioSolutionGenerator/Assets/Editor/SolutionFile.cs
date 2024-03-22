#nullable enable

using System;
using System.IO;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Represents a '.sln' file.
    /// </summary>
    internal sealed class SolutionFile
    {
        static SolutionFile()
        {
            var solutionDirectoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var solutionFilePath = $"{Path.Combine(solutionDirectoryPath, Path.GetFileName(solutionDirectoryPath))}.sln";
            CurrentProjectSolution = new SolutionFile(solutionDirectoryPath, solutionFilePath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFile"/> class.
        /// </summary>
        /// <param name="solutionDirectoryPath">The directory that contains the .sln file.</param>
        /// <param name="solutionFilePath">The full path of the .sln file.</param>
        public SolutionFile(string solutionDirectoryPath, string solutionFilePath)
        {
            SolutionDirectoryPath = solutionDirectoryPath ?? throw new ArgumentNullException(nameof(solutionDirectoryPath));
            SolutionFilePath = solutionFilePath ?? throw new ArgumentNullException(nameof(solutionFilePath));
        }

        /// <summary>
        ///     Gets the information about the '.sln' file of the current Unity Project.
        /// </summary>
        public static SolutionFile CurrentProjectSolution { get; }

        /// <summary>
        ///     Gets the absolute path to the directory containing the '.sln' file.
        /// </summary>
        public string SolutionDirectoryPath { get; }

        /// <summary>
        ///     Gets the absolute path to the '.sln' file.
        /// </summary>
        public string SolutionFilePath { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Path.GetFileName(SolutionFilePath);
        }
    }
}
