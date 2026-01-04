#nullable enable

using System;
using System.IO;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Represents a '.sln' file.
    /// </summary>
    internal sealed class SolutionFile
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SolutionFile" /> class.
        /// </summary>
        /// <param name="solutionDirectoryPath">The directory that contains the .sln file.</param>
        /// <param name="solutionFilePath">The full path of the .sln file.</param>
        public SolutionFile(string solutionDirectoryPath, string solutionFilePath)
        {
            SolutionDirectoryPath = solutionDirectoryPath ?? throw new ArgumentNullException(nameof(solutionDirectoryPath));
            SolutionFilePath = solutionFilePath ?? throw new ArgumentNullException(nameof(solutionFilePath));
        }

        /// <summary>
        ///     Gets or sets the information about the '.sln' of '.slnx' file of the current Unity Project.
        /// </summary>
        public static SolutionFile? CurrentProjectSolution { get; set; }

        /// <summary>
        ///     Gets the absolute path to the directory containing the '.sln' file.
        /// </summary>
        public string SolutionDirectoryPath { get; }

        /// <summary>
        ///     Gets the absolute path to the '.sln' file.
        /// </summary>
        public string SolutionFilePath { get; }

        /// <summary>
        ///     Gets a value indicating whether the solution file is in XML format (.slnx).
        /// </summary>
        public bool IsXmlSolution => Path.GetExtension(SolutionFilePath.AsSpan()).Equals(".slnx", StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override string ToString()
        {
            return Path.GetFileName(SolutionFilePath);
        }
    }
}
