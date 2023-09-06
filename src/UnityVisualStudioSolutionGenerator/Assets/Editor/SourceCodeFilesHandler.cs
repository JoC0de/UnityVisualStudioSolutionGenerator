#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[assembly: InternalsVisibleTo("UnityVisualStudioSolutionGenerator.Tests")]

namespace UnityVisualStudioSolutionGenerator
{
    internal static class SourceCodeFilesHandler
    {
        private static readonly byte[] Utf8BomBytes = { 0xEF, 0xBB, 0xBF };

        private static readonly byte[] EnableNullableBytes =
        {
            0x23, 0x6E, 0x75, 0x6C, 0x6C, 0x61, 0x62, 0x6C, 0x65, 0x20, 0x65, 0x6E, 0x61, 0x62, 0x6C, 0x65,
        };

        public static void EnableNullableOnAllFiles(SolutionFile solutionFile)
        {
            Debug.Assert(Encoding.UTF8.GetBytes("#nullable enable").SequenceEqual(EnableNullableBytes), "Wrong enableNullableBytes detected.");

            var allProjects = SolutionFileParser.Parse(solutionFile, false);
            var enableNullableReadBuffer = new byte[EnableNullableBytes.Length + 2];

            // source code is small so we can read it into memory
            var fullFileReadBuffer = new MemoryStream();
            foreach (var projectFile in allProjects)
            {
                var originalProjectFilePath = Path.Combine(solutionFile.SolutionDirectoryPath, Path.GetFileName(projectFile.FilePath));
                if (!File.Exists(originalProjectFilePath))
                {
                    LogHelper.LogWarning($"Can't find original (Unity) .csproj file at: '{originalProjectFilePath}'.");
                }

                var projectFileParser = new ProjectFileParser(originalProjectFilePath);
                foreach (var sourceCodeFile in projectFileParser.GetAllNonPackageSourceCodeFiles())
                {
                    AddNullableToFile(sourceCodeFile, enableNullableReadBuffer, fullFileReadBuffer);
                }
            }
        }

        private static void AddNullableToFile(string sourceCodeFile, byte[] enableNullableReadBuffer, MemoryStream fullFileReadBuffer)
        {
            try
            {
                using var fileStream = File.Open(sourceCodeFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete | FileShare.Read);
                var readCount = fileStream.Read(enableNullableReadBuffer);
                var readBytes = enableNullableReadBuffer.AsSpan(0, readCount);
                var readBytesWithoutBom = readBytes;
                var hasUtf8Bom = readBytes.SequenceEqual(Utf8BomBytes);
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

                fullFileReadBuffer.Write(readBytes);
                fileStream.CopyTo(fullFileReadBuffer);

                var newLineBytes = DetectNewLineBytes(fullFileReadBuffer);
                fullFileReadBuffer.Position = 0;

                // reset to start -> write enable nullable -> rest of file
                fileStream.Position = 0;
                fileStream.Write(EnableNullableBytes);
                var firstCharWasNewline = readBytes.StartsWith(newLineBytes);
                if (!firstCharWasNewline)
                {
                    fileStream.Write(newLineBytes);
                }

                var secondCharWasNewLine = readBytes.IsEmpty ||
                                           firstCharWasNewline &&
                                           (readBytes[Math.Min(newLineBytes.Length, readBytes.Length)..].StartsWith(newLineBytes) ||
                                            readBytes.Length == newLineBytes.Length);
                if (!secondCharWasNewLine)
                {
                    fileStream.Write(newLineBytes);
                }

                fullFileReadBuffer.CopyTo(fileStream);
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
    }
}
