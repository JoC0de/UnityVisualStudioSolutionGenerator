#nullable enable

using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Generates a C# project file in the new sdk style (the new style is currently not directly supported by the Unity Visual Studio Plugin).
    /// </summary>
    public class ProjectFileGeneratorSdkStyle : ProjectFileGeneratorBase
    {
        /// <inheritdoc cref="ProjectFileGeneratorBase(System.Xml.Linq.XDocument,string)" />
        public ProjectFileGeneratorSdkStyle(XDocument document, string filePath)
            : base(document, filePath)
        {
        }

        /// <inheritdoc />
        protected override void WriteProjectFileInternal(XmlWriter writer, string outputFileDirectoryPath)
        {
            using var wrappedWriter = new XmlWriterWithoutNamespace(writer);

            wrappedWriter.WriteStartElement("Project");

            wrappedWriter.WriteStartElement("PropertyGroup");
            wrappedWriter.WriteElementString("OutputPath", Path.Combine(SolutionDirectoryPath, "Temp", "Bin", "$(Configuration)", ProjectName));
            wrappedWriter.WriteElementString("BaseIntermediateOutputPath", Path.Combine(SolutionDirectoryPath, "Temp", "Obj", ProjectName));
            wrappedWriter.WriteElementString("IsUnityProject", "true");
            wrappedWriter.WriteEndElement(); // </PropertyGroup>

            var relativeSubProjectDirectories = FindSubProjectFolders(outputFileDirectoryPath);
            wrappedWriter.WriteStartElement("PropertyGroup");
            var fileExcludedPatterns = string.Join(
                ';',
                GeneratorSettings.SdkExcludedFilePatterns.Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                    .Select(pattern => pattern.Trim())
                    .Concat(relativeSubProjectDirectories.Select(relativeSubProjectDirectory => $"{relativeSubProjectDirectory}/**")));
            wrappedWriter.WriteElementString("DefaultItemExcludes", $"$(DefaultItemExcludes);{fileExcludedPatterns}");
            wrappedWriter.WriteElementString("ImplicitUsings", "disable");

            // unity explicitly imports all 'framework' DLLs so we need to disable importing them
            wrappedWriter.WriteElementString("DisableImplicitFrameworkReferences", "true");
            wrappedWriter.WriteElementString("NoConfig", "true");
            wrappedWriter.WriteElementString("NoStdLib", "true");
            wrappedWriter.WriteElementString("NoStandardLibraries", "true");

            foreach (var additionalProperty in GeneratorSettings.SdkAdditionalProperties.Where(property => !string.IsNullOrWhiteSpace(property.Name)))
            {
                wrappedWriter.WriteElementString(additionalProperty.Name.Trim(), additionalProperty.Value.Trim());
            }

            wrappedWriter.WriteEndElement(); // </PropertyGroup>

            // copy property groups elements from input
            foreach (var propertyGroup in ProjectElement.Elements(XmlNamespace + "PropertyGroup"))
            {
                propertyGroup.Element(XmlNamespace + "OutputPath")?.Remove();
                propertyGroup.Element(XmlNamespace + "ProductVersion")?.Remove();
                propertyGroup.Element(XmlNamespace + "SchemaVersion")?.Remove();

                // we can use 'whatever we want' it only affects how visual studio threats the solution
                // see: https://github.com/needle-mirror/com.unity.ide.visualstudio/blob/d1e4dd05ad818112d067dd465b5b3c387498fdc7/Editor/ProjectGeneration/SdkStyleProjectGeneration.cs#L51
                var targetFrameworkVersionElement = propertyGroup.Element(XmlNamespace + "TargetFrameworkVersion");
                targetFrameworkVersionElement?.ReplaceWith(new XElement(XmlNamespace + "TargetFramework", "netstandard2.1"));
                propertyGroup.WriteTo(wrappedWriter);
            }

            // we need to import the 'Framework' explicitly after setting 'BaseIntermediateOutputPath' see https://github.com/dotnet/msbuild/issues/1603
            // and https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk#use-the-import-element-anywhere-in-your-project
            wrappedWriter.WriteStartElement("Import");
            wrappedWriter.WriteAttributeString("Project", "Sdk.props");
            wrappedWriter.WriteAttributeString("Sdk", "Microsoft.NET.Sdk");
            wrappedWriter.WriteEndElement(); // </Import>

            // include *.asmdef file explicitly so we can find it later
            wrappedWriter.WriteStartElement("ItemGroup");
            wrappedWriter.WriteStartElement("None");
            wrappedWriter.WriteAttributeString("Include", Path.GetRelativePath(outputFileDirectoryPath, AssemblyDefinitionFilePath));
            wrappedWriter.WriteEndElement(); // </None>
            wrappedWriter.WriteEndElement(); // </ItemGroup>

            // copy Analyzer or Reference (dll) imports
            foreach (var itemGroup in ProjectElement.Elements(XmlNamespace + "ItemGroup"))
            {
                if (!itemGroup.Elements(XmlNamespace + "Analyzer").Any() && !itemGroup.Elements(XmlNamespace + "Reference").Any())
                {
                    continue;
                }

                wrappedWriter.WriteStartElement("ItemGroup");
                foreach (var includedItem in itemGroup.Elements(XmlNamespace + "Analyzer").Concat(itemGroup.Elements(XmlNamespace + "Reference")))
                {
                    var hintPathElement = includedItem.Element(XmlNamespace + "HintPath");
                    if (hintPathElement != null)
                    {
                        hintPathElement.Value = Path.GetFullPath(hintPathElement.Value, SolutionDirectoryPath);
                    }

                    includedItem.WriteTo(wrappedWriter);
                }

                wrappedWriter.WriteEndElement(); // </ItemGroup>
            }

            // copy ProjectReference's
            wrappedWriter.WriteStartElement("ItemGroup");
            foreach (var projectReferenceElement in ProjectElement.Descendants(XmlNamespace + "ProjectReference"))
            {
                var includeAttribute = projectReferenceElement.Attribute("Include");
                if (includeAttribute is null)
                {
                    continue;
                }

                var currentProjectFilePath = Path.GetFullPath(includeAttribute.Value, SolutionDirectoryPath);
                var newProjectFilePath = DetermineNewProjectFilePath(currentProjectFilePath);
                includeAttribute.Value = Path.GetRelativePath(outputFileDirectoryPath, newProjectFilePath);
                projectReferenceElement.WriteTo(wrappedWriter);
            }

            wrappedWriter.WriteEndElement(); // </ItemGroup>

            // targets are should be the last item
            wrappedWriter.WriteStartElement("Import");
            wrappedWriter.WriteAttributeString("Project", "Sdk.targets");
            wrappedWriter.WriteAttributeString("Sdk", "Microsoft.NET.Sdk");
            wrappedWriter.WriteEndElement(); // </Import>

            wrappedWriter.WriteEndElement(); // </Project>
            wrappedWriter.Flush();
            LogHelper.LogVerbose($"Generated .csproj file for Project: {ProjectName}");
        }

        private sealed class XmlWriterWithoutNamespace : XmlWriter
        {
            private readonly XmlWriter implementation;

            public XmlWriterWithoutNamespace(XmlWriter implementation)
            {
                this.implementation = implementation;
            }

            public override WriteState WriteState => implementation.WriteState;

            public override void Flush()
            {
                implementation.Flush();
            }

            public override string? LookupPrefix(string ns)
            {
                return implementation.LookupPrefix(ns);
            }

            public override void WriteBase64(byte[] buffer, int index, int count)
            {
                implementation.WriteBase64(buffer, index, count);
            }

            public override void WriteCData(string text)
            {
                implementation.WriteCData(text);
            }

            public override void WriteCharEntity(char ch)
            {
                implementation.WriteCharEntity(ch);
            }

            public override void WriteChars(char[] buffer, int index, int count)
            {
                implementation.WriteChars(buffer, index, count);
            }

            public override void WriteComment(string text)
            {
                implementation.WriteComment(text);
            }

            public override void WriteDocType(string name, string pubid, string sysid, string subset)
            {
                implementation.WriteDocType(name, pubid, sysid, subset);
            }

            public override void WriteEndAttribute()
            {
                implementation.WriteEndAttribute();
            }

            public override void WriteEndDocument()
            {
                implementation.WriteEndDocument();
            }

            public override void WriteEndElement()
            {
                implementation.WriteEndElement();
            }

            public override void WriteEntityRef(string name)
            {
                implementation.WriteEntityRef(name);
            }

            public override void WriteFullEndElement()
            {
                implementation.WriteFullEndElement();
            }

            public override void WriteProcessingInstruction(string name, string text)
            {
                implementation.WriteProcessingInstruction(name, text);
            }

            public override void WriteRaw(char[] buffer, int index, int count)
            {
                implementation.WriteRaw(buffer, index, count);
            }

            public override void WriteRaw(string data)
            {
                implementation.WriteRaw(data);
            }

            public override void WriteStartAttribute(string prefix, string localName, string ns)
            {
                implementation.WriteStartAttribute(prefix, localName, ns);
            }

            public override void WriteStartDocument()
            {
                implementation.WriteStartDocument();
            }

            public override void WriteStartDocument(bool standalone)
            {
                implementation.WriteStartDocument(standalone);
            }

            public override void WriteStartElement(string prefix, string localName, string ns)
            {
                implementation.WriteStartElement(prefix, localName, string.Empty);
            }

            public override void WriteString(string text)
            {
                implementation.WriteString(text);
            }

            public override void WriteSurrogateCharEntity(char lowChar, char highChar)
            {
                implementation.WriteSurrogateCharEntity(lowChar, highChar);
            }

            public override void WriteWhitespace(string ws)
            {
                implementation.WriteWhitespace(ws);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                implementation.Dispose();
            }
        }

        // ReSharper disable CommentTypo
        // Ignore Spelling: pubid, sysid
        // ReSharper restore CommentTypo
    }
}
