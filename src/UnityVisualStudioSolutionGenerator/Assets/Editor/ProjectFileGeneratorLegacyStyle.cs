#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Generates a C# project file in the legacy style (the style currently used by the Unity Visual Studio Plugin).
    /// </summary>
    public class ProjectFileGeneratorLegacyStyle : ProjectFileGeneratorBase
    {
        /// <inheritdoc cref="ProjectFileGeneratorBase(string)" />
        public ProjectFileGeneratorLegacyStyle(string filePath)
            : base(filePath)
        {
        }

        /// <inheritdoc />
        protected override void WriteProjectFileInternal(XmlWriter writer, string outputFileDirectoryPath, string solutionDirectoryPath)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStartDocument();

            foreach (var outputPathElement in ProjectElement.Descendants(XmlNamespace + "OutputPath"))
            {
                outputPathElement.Value = Path.Combine(solutionDirectoryPath, "Temp", "Bin", "$(Configuration)", ProjectName);
            }

            foreach (var hintPathElement in ProjectElement.Descendants(XmlNamespace + "HintPath"))
            {
                hintPathElement.Value = Path.GetFullPath(hintPathElement.Value, DirectoryPath);
            }

            ProjectElement.Descendants(XmlNamespace + "Compile").Remove();

            var compileIncludeAllElement = new XElement(XmlNamespace + "Compile", new XAttribute("Include", "**/*.cs"));
            var firstItemGroupElement = new XElement(XmlNamespace + "ItemGroup", compileIncludeAllElement);
            ProjectElement.Elements(XmlNamespace + "ItemGroup").First().AddBeforeSelf(firstItemGroupElement);

            // if there are sup-folders with a assembly definition file (a sup project) we need to ignore its files, they are imported as a project reference.
            var foldersToIgnore = FindSubProjectFolders(outputFileDirectoryPath);
            compileIncludeAllElement.AddAfterSelf(
                foldersToIgnore.Select(
                    relativeSubProjectDirectory => new XElement(
                        XmlNamespace + "Compile",
                        new XAttribute("Remove", $"{relativeSubProjectDirectory}/**/*.cs"))));

            foreach (var noneElement in ProjectElement.Descendants(XmlNamespace + "None"))
            {
                var includeAttribute = noneElement.Attribute("Include");
                if (includeAttribute is null)
                {
                    continue;
                }

                includeAttribute.Value = Path.GetRelativePath(outputFileDirectoryPath, Path.GetFullPath(includeAttribute.Value, DirectoryPath));
                noneElement.Element(XmlNamespace + "Link")?.Remove();
            }

            foreach (var projectReferenceElement in ProjectElement.Descendants(XmlNamespace + "ProjectReference"))
            {
                var includeAttribute = projectReferenceElement.Attribute("Include");
                if (includeAttribute is null)
                {
                    continue;
                }

                var currentProjectFilePath = Path.GetFullPath(includeAttribute.Value, DirectoryPath);
                var newProjectFilePath = DetermineNewProjectFilePath(currentProjectFilePath);
                includeAttribute.Value = Path.GetRelativePath(outputFileDirectoryPath, newProjectFilePath);
            }

            ProjectElement.AddFirst(
                new XElement(
                    XmlNamespace + "PropertyGroup",
                    new XElement(XmlNamespace + "BaseIntermediateOutputPath", Path.Combine(solutionDirectoryPath, "obj", "Legacy", ProjectName)),
                    new XElement(XmlNamespace + "EnableNETAnalyzers", "true"),
                    new XElement(XmlNamespace + "AnalysisLevel", "latest"),
                    new XElement(XmlNamespace + "AnalysisMode", "AllEnabledByDefault")));

            ProjectElement.WriteTo(writer);

            writer.WriteEndDocument();
            writer.Flush();

            LogHelper.LogVerbose($"Generated project file: {ProjectName}.csproj");
        }
    }
}
