#nullable enable

using System;

namespace UnityVisualStudioSolutionGenerator
{
    public sealed class ProjectFile
    {
        public ProjectFile(string filePath, string id)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public string FilePath { get; }

        public string Id { get; }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ProjectFile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode(StringComparison.Ordinal);
        }

        private bool Equals(ProjectFile other)
        {
            return Id == other.Id;
        }
    }
}
