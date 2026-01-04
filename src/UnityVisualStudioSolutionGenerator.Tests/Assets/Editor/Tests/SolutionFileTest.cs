#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace UnityVisualStudioSolutionGenerator.Tests
{
    public class SolutionFileTest
    {
        private static readonly string TestSolutionContent = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.32014.148
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""UnityVisualStudioSolutionGenerator"", ""../UnityVisualStudioSolutionGenerator/Assets/Editor/UnityVisualStudioSolutionGenerator.csproj"", ""{91E92C9C-24FB-0F81-A436-50F1D483A5F4}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""UnityVisualStudioSolutionGenerator.Tests"", ""Assets/Editor/Tests/UnityVisualStudioSolutionGenerator.Tests.csproj"", ""{50330BEB-948C-A36A-8379-F379D799AF9A}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{91E92C9C-24FB-0F81-A436-50F1D483A5F4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{91E92C9C-24FB-0F81-A436-50F1D483A5F4}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{91E92C9C-24FB-0F81-A436-50F1D483A5F4}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{91E92C9C-24FB-0F81-A436-50F1D483A5F4}.Release|Any CPU.Build.0 = Release|Any CPU
		{50330BEB-948C-A36A-8379-F379D799AF9A}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{50330BEB-948C-A36A-8379-F379D799AF9A}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{50330BEB-948C-A36A-8379-F379D799AF9A}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{50330BEB-948C-A36A-8379-F379D799AF9A}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
".Replace('/', Path.DirectorySeparatorChar);

        private static readonly string SolutionDirectoryPath = Path.GetFullPath(Directory.GetCurrentDirectory());

        private static readonly ProjectFile[] TestSolutionProjectFiles =
        {
            new(
                Path.GetFullPath(
                    Path.Combine(
                        SolutionDirectoryPath,
                        "../UnityVisualStudioSolutionGenerator/Assets/Editor/UnityVisualStudioSolutionGenerator.csproj")),
                "{91E92C9C-24FB-0F81-A436-50F1D483A5F4}"),
            new(
                Path.GetFullPath(Path.Combine(SolutionDirectoryPath, "Assets/Editor/Tests/UnityVisualStudioSolutionGenerator.Tests.csproj")),
                "{50330BEB-948C-A36A-8379-F379D799AF9A}"),
        };

        [Test]
        public void ParseSolutionTest()
        {
            var solutionFile = new SolutionFile(SolutionDirectoryPath, "UnityVisualStudioSolutionGenerator.Tests.sln");
            var (projectFiles, _) = SolutionFileParser.Parse(TestSolutionContent, solutionFile, false);

            Assert.That(projectFiles, Is.EqualTo(TestSolutionProjectFiles).Using(new ProjectFileEqualityComparer()));
        }

        [Test]
        public void WriteSolutionTest()
        {
            var solutionFile = new SolutionFile(SolutionDirectoryPath, "UnityVisualStudioSolutionGenerator.Tests.sln");
            var generatedSolutionContent = SolutionFileWriter.WriteToText(solutionFile, TestSolutionProjectFiles);

            Assert.That(generatedSolutionContent, Is.EqualTo(TestSolutionContent));
        }

        private sealed class ProjectFileEqualityComparer : IEqualityComparer<ProjectFile>
        {
            public bool Equals(ProjectFile? x, ProjectFile? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return x.FilePath == y.FilePath && x.Id == y.Id;
            }

            public int GetHashCode(ProjectFile obj)
            {
                return HashCode.Combine(obj.FilePath, obj.Id);
            }
        }
    }
}
