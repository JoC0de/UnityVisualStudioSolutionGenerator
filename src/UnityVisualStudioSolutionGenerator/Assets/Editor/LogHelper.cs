#nullable enable

using System;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Helper for writing logs to Unity <see cref="Debug.LogFormat(string,object[])" />.
    /// </summary>
    internal static class LogHelper
    {
        /// <summary>
        ///     Logs a informational message. A message that is displayed by default.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInformation(FormattableString message)
        {
            Log(LogType.Log, message);
        }

        /// <summary>
        ///     Logs a error message. Always shown in the log and has a extra highlight.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(FormattableString message)
        {
            Log(LogType.Error, message);
        }

        /// <summary>
        ///     Logs a error message. Always shown in the log and has a extra highlight.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(FormattableString message)
        {
            Log(LogType.Warning, message);
        }

        /// <summary>
        ///     Logs a verbose message. A message that is only shown if <see cref="GeneratorSettings.LogVerbose" /> is enabled.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogVerbose(FormattableString message)
        {
            if (GeneratorSettings.LogVerbose)
            {
                LogInformation(message);
            }
        }

        private static void Log(LogType logType, FormattableString message)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, message.Format, message.GetArguments());
        }
    }
}
