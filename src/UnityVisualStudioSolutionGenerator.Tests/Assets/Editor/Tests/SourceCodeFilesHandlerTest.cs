#nullable enable

using System;
using System.IO;
using NUnit.Framework;

namespace UnityVisualStudioSolutionGenerator.Tests
{
    public class SourceCodeFilesHandlerTest
    {
        private const string TestSolutionContent = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.32014.148
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""TestProject"", ""TestProject.csproj"", ""{91E92C9C-24FB-0F81-A436-50F1D483A5F4}""
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
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";

        private const string TestProjectFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <Compile Include=""TestSourceCode.cs"">
      <Link>Editor\TestSourceCode.cs</Link>
    </Compile>
  </ItemGroup>
</Project>";

        [TestCase("\r\n", "#nullable enable\r\n")]
        [TestCase("\n", "#nullable enable\n")]
        [TestCase("", "#nullable enable\n")]
        [TestCase("a", "#nullable enable\n\na")]
        [TestCase("public class Test\n{\n}\n", "#nullable enable\n\npublic class Test\n{\n}\n")]
        [TestCase("#nullable enable", "#nullable enable")]
        [TestCase("#nullable enable\n", "#nullable enable\n")]
        [TestCase("#nullable enable\r\n", "#nullable enable\r\n")]
        [TestCase("#nullable enable\n\npublic class Test\n{\n}\n", "#nullable enable\n\npublic class Test\n{\n}\n")]
        public void SourceCodeFilesHandlerSimpleTest(string testSourceCode, string expectedSourceCode)
        {
            if (!testSourceCode.Contains('\n', StringComparison.Ordinal))
            {
                expectedSourceCode = expectedSourceCode.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
            }

            const string testProjectFilePath = "TestProject.csproj";
            const string testSourceCodeFilePath = "TestSourceCode.cs";
            var testSolutionDirectoryPath = Directory.GetCurrentDirectory();
            var testSolutionFilePath = Path.Combine(testSolutionDirectoryPath, "test.sln");
            try
            {
                File.WriteAllText(testSolutionFilePath, TestSolutionContent);
                File.WriteAllText(testProjectFilePath, TestProjectFileContent);
                File.WriteAllText(testSourceCodeFilePath, testSourceCode);
                SourceCodeFilesHandler.EnableNullableOnAllFiles(new SolutionFile(testSolutionDirectoryPath, testSolutionFilePath));

                var generatedSourceCode = File.ReadAllText(testSourceCodeFilePath);
                Assert.That(generatedSourceCode, Is.EqualTo(expectedSourceCode));
            }
            finally
            {
                File.Delete(testSolutionFilePath);
                File.Delete(testProjectFilePath);
                File.Delete(testSourceCodeFilePath);
            }
        }

        [TestCase("public class Test\n{\n}\n", "#nullable enable\n\npublic class Test\n{\n}\n")]
        [TestCase("#nullable enable\n\npublic class Test\n{\n}\n", "#nullable enable\n\npublic class Test\n{\n}\n")]
        public void SourceCodeFilesHandlerMultiFileTest(string testSourceCode, string expectedSourceCode)
        {
            if (!testSourceCode.Contains('\n', StringComparison.Ordinal))
            {
                expectedSourceCode = expectedSourceCode.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
            }

            const string testProjectFilePath = "TestProject.csproj";
            const string testSourceCode1FilePath = "TestSourceCode.cs";
            const string testSourceCode2FilePath = "TestSourceCode2.cs";
            var testSolutionDirectoryPath = Directory.GetCurrentDirectory();
            var testSolutionFilePath = Path.Combine(testSolutionDirectoryPath, "test.sln");
            try
            {
                File.WriteAllText(testSolutionFilePath, TestSolutionContent);
                File.WriteAllText(
                    testProjectFilePath,
                    TestProjectFileContent.Replace(
                        "</ItemGroup>",
                        $"  <Compile Include=\"{testSourceCode2FilePath}\">\r\n    <Link>Editor\\{testSourceCode2FilePath}</Link>\r\n  </Compile>\r\n</ItemGroup>",
                        StringComparison.Ordinal));
                File.WriteAllText(testSourceCode1FilePath, testSourceCode);
                File.WriteAllText(testSourceCode2FilePath, testSourceCode);
                SourceCodeFilesHandler.EnableNullableOnAllFiles(new SolutionFile(testSolutionDirectoryPath, testSolutionFilePath));

                var generatedSourceCode1 = File.ReadAllText(testSourceCode1FilePath);
                Assert.That(generatedSourceCode1, Is.EqualTo(expectedSourceCode));
                var generatedSourceCode2 = File.ReadAllText(testSourceCode2FilePath);
                Assert.That(generatedSourceCode2, Is.EqualTo(expectedSourceCode));
            }
            finally
            {
                File.Delete(testSolutionFilePath);
                File.Delete(testProjectFilePath);
                File.Delete(testSourceCode1FilePath);
                File.Delete(testSourceCode2FilePath);
            }
        }
    }
}
