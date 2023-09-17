#nullable enable

using System;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Represents a C# project file.
    /// </summary>
    public sealed class ProjectFile
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProjectFile" /> class.
        /// </summary>
        /// <param name="filePath">The absolute path of the project file.</param>
        /// <param name="id">The ID of the project file.</param>
        public ProjectFile(string filePath, string id)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>
        ///     Gets the absolute path of the project file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     Gets the ID of the project file.
        /// </summary>
        public string Id { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ProjectFile other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode(StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(FilePath)}: {FilePath}, {nameof(Id)}: {Id}";
        }

        private bool Equals(ProjectFile other)
        {
            // we need to use the FilePath as an alternative so we detect duplicate entries inside .sln
            return Id == other.Id || FilePath == other.FilePath;
        }
    }
}
