#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides methods for writing Visual Studio solution files in XML format (.slnx).
    /// </summary>
    public static class XmlSolutionFileWriter
    {
        /// <summary>
        ///     Writes a Visual Studio solution file in XML format (.slnx) to the specified writer.
        /// </summary>
        /// <param name="writer">The text writer to write the solution content to.</param>
        /// <param name="solutionDirectoryPath">The absolute directory path of the solution file.</param>
        /// <param name="allProjects">All projects that should be included inside the solution.</param>
        public static void WriteTo(TextWriter writer, string solutionDirectoryPath, IReadOnlyList<ProjectFile> allProjects)
        {
            _ = allProjects ?? throw new ArgumentNullException(nameof(allProjects));

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = SolutionFileWriter.WindowsNewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
            };

            using var xmlWriter = XmlWriter.Create(writer, settings);
            xmlWriter.WriteStartElement("Solution");

            foreach (var project in allProjects)
            {
                var relative = Path.GetRelativePath(solutionDirectoryPath, project.FilePath);
                xmlWriter.WriteStartElement("Project");
                xmlWriter.WriteAttributeString("Path", relative);
                xmlWriter.WriteEndElement(); // Project
            }

            xmlWriter.WriteEndElement(); // Solution
            xmlWriter.Flush();
        }
    }
}
