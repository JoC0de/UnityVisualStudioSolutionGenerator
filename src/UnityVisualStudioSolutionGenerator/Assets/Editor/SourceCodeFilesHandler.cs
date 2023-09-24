#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[assembly: InternalsVisibleTo("UnityVisualStudioSolutionGenerator.Tests")]

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides functionalities for manipulation source code files.
    /// </summary>
    internal static class SourceCodeFilesHandler
    {
        private static readonly byte[] Utf8BomBytes = { 0xEF, 0xBB, 0xBF };

        private static readonly byte[] EnableNullableBytes =
        {
            0x23, 0x6E, 0x75, 0x6C, 0x6C, 0x61, 0x62, 0x6C, 0x65, 0x20, 0x65, 0x6E, 0x61, 0x62, 0x6C, 0x65,
        };

        /// <summary>
        ///     Adds the '#nullable enable' to all .cs files of all projects inside the <paramref name="solutionFile" />.
        /// </summary>
        /// <param name="solutionFile">The Visual Studio Solution file of witch all .cs files should be manipulated.</param>
        public static void EnableNullableOnAllFiles(SolutionFile solutionFile)
        {
            Debug.Assert(Encoding.UTF8.GetBytes("#nullable enable").SequenceEqual(EnableNullableBytes), "Wrong enableNullableBytes detected.");

            var (allProjects, _) = SolutionFileParser.Parse(solutionFile, false);
            var enableNullableReadBuffer = new byte[EnableNullableBytes.Length + Utf8BomBytes.Length];

            // source code is small so we can read it into memory
            var fullFileReadBuffer = new MemoryStream();
            foreach (var projectFile in allProjects)
            {
                var originalProjectFilePath = Path.Combine(solutionFile.SolutionDirectoryPath, Path.GetFileName(projectFile.FilePath));
                if (!File.Exists(originalProjectFilePath))
                {
                    var generatedProjectFileParser = new ProjectFileParser(projectFile.FilePath);
                    var assemblyDefinitionContent =
                        JsonUtility.FromJson<AssemblyDefinitionContent>(File.ReadAllText(generatedProjectFileParser.AssemblyDefinitionFilePath));
                    originalProjectFilePath = Path.Combine(solutionFile.SolutionDirectoryPath, $"{assemblyDefinitionContent.name}.csproj");
                    if (!File.Exists(originalProjectFilePath))
                    {
                        LogHelper.LogWarning($"Can't find original (Unity) .csproj file at: '{originalProjectFilePath}'.");
                    }
                }

                var projectFileParser = new ProjectFileParser(originalProjectFilePath);
                foreach (var sourceCodeFile in projectFileParser.GetAllNonPackageSourceCodeFiles())
                {
                    AddNullableToFile(sourceCodeFile, enableNullableReadBuffer, fullFileReadBuffer);
                }
            }
        }

        /// <summary>
        ///     Enables nullable of a single file. Adds '#nullable enable' to the top of the file.
        /// </summary>
        /// <param name="sourceCodeFile">The path to the file to add nullable setting to, if it doesn't already has it.</param>
        public static void AddNullableToFile(string sourceCodeFile)
        {
            AddNullableToFile(sourceCodeFile, new byte[EnableNullableBytes.Length + Utf8BomBytes.Length], new MemoryStream());
        }

        private static void AddNullableToFile(string sourceCodeFile, byte[] enableNullableReadBuffer, MemoryStream fullFileReadBuffer)
        {
            try
            {
                using var fileStream = File.Open(sourceCodeFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete | FileShare.Read);
                var readCount = fileStream.Read(enableNullableReadBuffer);
                var readBytes = enableNullableReadBuffer.AsSpan(0, readCount);
                var readBytesWithoutBom = readBytes;
                var hasUtf8Bom = readBytes.StartsWith(Utf8BomBytes);
                if (hasUtf8Bom)
                {
                    readBytesWithoutBom = readBytes[Utf8BomBytes.Length..];
                }

                if (readBytesWithoutBom.StartsWith(EnableNullableBytes))
                {
                    // #nullable found
                    return;
                }

                fullFileReadBuffer.SetLength(0);
                if (fullFileReadBuffer.Capacity < fileStream.Length)
                {
                    fullFileReadBuffer.Capacity = (int)fileStream.Length;
                }

                fullFileReadBuffer.Write(readBytesWithoutBom);
                fileStream.CopyTo(fullFileReadBuffer);

                var newLineBytes = DetectNewLineBytes(fullFileReadBuffer);
                fullFileReadBuffer.Position = 0;

                // reset to start -> write enable nullable -> rest of file
                fileStream.Position = 0;

                if (hasUtf8Bom)
                {
                    fileStream.Write(Utf8BomBytes);
                }

                fileStream.Write(EnableNullableBytes);
                var firstCharWasNewline = readBytesWithoutBom.StartsWith(newLineBytes);
                if (!firstCharWasNewline)
                {
                    fileStream.Write(newLineBytes);
                }

                var secondCharWasNewLine = readBytesWithoutBom.IsEmpty ||
                                           firstCharWasNewline &&
                                           (readBytesWithoutBom[Math.Min(newLineBytes.Length, readBytesWithoutBom.Length)..]
                                                .StartsWith(newLineBytes) ||
                                            readBytesWithoutBom.Length == newLineBytes.Length);
                if (!secondCharWasNewLine)
                {
                    fileStream.Write(newLineBytes);
                }

                fullFileReadBuffer.CopyTo(fileStream);
                LogHelper.LogVerbose($"Added '#nullable enable' to file: '{sourceCodeFile}'.");
            }
            catch (Exception e) when (e is IOException or ArgumentException or InvalidOperationException or AccessViolationException
                                          or IndexOutOfRangeException)
            {
                LogHelper.LogError($"Failed to write '#nullable enable' to file '{sourceCodeFile}'. Got error: {e}");
            }
        }

        private static byte[] DetectNewLineBytes(MemoryStream memoryStream)
        {
            memoryStream.Position = 0;
            var previous = 0;

            while (true)
            {
                var readByte = memoryStream.ReadByte();
                if (readByte == -1)
                {
                    // can't detect -> fallback
                    return Encoding.UTF8.GetBytes(Environment.NewLine);
                }

                if (readByte == (byte)'\n')
                {
                    return previous == '\r' ? new[] { (byte)'\r', (byte)'\n' } : new[] { (byte)'\n' };
                }

                previous = readByte;
            }
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Instantiated by json-serializer.")]
        private sealed class AssemblyDefinitionContent
        {
            [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local", Justification = "Required by serializer.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by serializer.")]
            [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Required by serializer.")]
            [SuppressMessage(
                "StyleCop.CSharp.NamingRules",
                "SA1307:Accessible fields should begin with upper-case letter",
                Justification = "Required by serializer.")]
            [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Required by serializer.")]
            public string name = string.Empty;
        }
    }
}
