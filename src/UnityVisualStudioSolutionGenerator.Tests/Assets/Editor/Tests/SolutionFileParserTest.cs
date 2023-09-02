using System.IO;
using System.Linq;
using Microsoft.Unity.VisualStudio.Editor;
using NUnit.Framework;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator.Tests
{
    public class SolutionFileParserTest
    {
        [Test]
        public void ParseOwnSolutionTest()
        {
            new VisualStudioEditor().SyncAll();

            var solutionDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var projectFiles = SolutionFileParser.Parse(
                File.ReadAllText(Path.Combine(solutionDirectory, $"{Path.GetFileName(solutionDirectory)}.sln")),
                solutionDirectory,
                false);

            Assert.That(
                projectFiles.Select(projectFile => Path.GetFileName(projectFile.FilePath)),
                Is.EquivalentTo(new[] { "UnityVisualStudioSolutionGenerator.csproj", "UnityVisualStudioSolutionGenerator.Tests.csproj" }));
        }
    }
}
