using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator.Tests
{
    public class SolutionFileParserTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void ParseOwnSolutionTest()
        {
            var solutionDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var projectFiles = SolutionFileParser.Parse(
                File.ReadAllText(Path.Combine(solutionDirectory, $"{Path.GetFileName(solutionDirectory)}.sln")),
                solutionDirectory);

            Assert.That(
                projectFiles.Select(projectFile => Path.GetFileName(projectFile.FilePath)),
                Is.EquivalentTo(new[] { "UnityVisualStudioSolutionGenerator.csproj", "UnityVisualStudioSolutionGenerator.Tests.csproj" }));
        }
    }
}
