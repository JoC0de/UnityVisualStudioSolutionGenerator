#nullable enable

using System;
using System.IO;

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
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
        }

        /// <summary>
        ///     Gets the absolute path of the project file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     Gets the ID of the project file.
        ///     When using .slnx solution files we have no 'Id' it will always be empty string.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Gets the project name witch is the name of the project file without its extension.
        /// </summary>
        public string ProjectName { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ProjectFile other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (Id.Length == 0)
            {
                return ProjectName.GetHashCode(StringComparison.Ordinal);
            }

            return Id.GetHashCode(StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Id.Length == 0)
            {
                return $"{nameof(FilePath)}: {FilePath}";
            }

            return $"{nameof(FilePath)}: {FilePath}, {nameof(Id)}: {Id}";
        }

        private bool Equals(ProjectFile other)
        {
            if (Id.Length + other.Id.Length == 0)
            {
                // if using slnx the Id is empty.
                return ProjectName == other.ProjectName;
            }

            // we need to use the ProjectName as an alternative so we detect duplicate entries inside .sln
            return Id == other.Id || ProjectName == other.ProjectName;
        }
    }
}
