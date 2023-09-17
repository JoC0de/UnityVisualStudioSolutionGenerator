#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using UnityVisualStudioSolutionGenerator.Configuration;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     File system change watcher that watches all source code file changes inside the project.
    /// </summary>
    internal sealed class SourceCodeFileWatcher : IDisposable
    {
        private readonly FileSystemWatcher watcher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SourceCodeFileWatcher" /> class.
        /// </summary>
        /// <param name="folderPath">
        ///     The root directory of all project files. Is used as a starting point for the watcher, so only files inside this folder or
        ///     any sup-folder are watched.
        /// </param>
        public SourceCodeFileWatcher(string folderPath)
        {
            watcher = new FileSystemWatcher(folderPath, "*.cs") { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName };
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.EnableRaisingEvents = true;
            LogHelper.LogVerbose($"Created a new {nameof(SourceCodeFileWatcher)} for the directory: '{folderPath}'.");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            watcher.Dispose();
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            LogHelper.LogVerbose($"Received {nameof(OnDeleted)} event for file: '{e.FullPath}'.");
            if (e.FullPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || !GeneratorSettings.TrackMetaDeletion)
            {
                return;
            }

            try
            {
                File.Delete($"{e.FullPath}.meta");
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                LogHelper.LogVerbose($".meta file corresponding to: '{e.FullPath}' already deleted. Error:\n{exception}");
            }
            catch (Exception exception) when (exception is AccessViolationException or IOException)
            {
                LogHelper.LogWarning($"Failed to delete .meta corresponding to: '{e.FullPath}'. Error:\n{exception}");
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            LogHelper.LogVerbose($"Received {nameof(OnCreated)} event for file: '{e.FullPath}'.");
            if (e.FullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && GeneratorSettings.EnableNullableReferenceTypes)
            {
                AddNullableToFileDelayed(e.FullPath);
            }
        }

        [SuppressMessage(
            "Major Bug",
            "S3168:\"async\" methods should not return \"void\"",
            Justification = "We can't await because it is called by a event.")]
        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Exceptions are handled in 'AddNullableToFile'.")]
        private static async void AddNullableToFileDelayed(string filePath)
        {
            // we need to delay the task because Unity else produces warnings, about assets being modified while thy are imported, when we create a '.cs' file inside Unity Editor.
            await Task.Run(
                    async () =>
                    {
                        await Task.Delay(100).ConfigureAwait(true);
                        SourceCodeFilesHandler.AddNullableToFile(filePath);
                    })
                .ConfigureAwait(true);
        }
    }
}
