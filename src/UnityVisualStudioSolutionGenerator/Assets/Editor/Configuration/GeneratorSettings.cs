#nullable enable

using System.Collections.Generic;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Provides settings used for generating the Visual Studio Solution File.
    /// </summary>
    public static class GeneratorSettings
    {
        [UserSetting("General Settings", "Enabled", "If this is unchecked the 'normal' Unity solution generator will be used.")]
        private static readonly GeneratorSettingsValue<bool> IsEnabledSetting = new($"general.{nameof(IsEnabled)}", true);

        [UserSetting("General Settings", "Generate SDK-Style Projects")]
        private static readonly GeneratorSettingsValue<bool> GenerateSdkStyleProjectsSetting = new(
            $"general.{nameof(GenerateSdkStyleProjects)}",
            true);

        [UserSetting("General Settings", "Enable verbose logging")]
        private static readonly GeneratorSettingsValue<bool> LogVerboseSetting = new($"general.{nameof(LogVerbose)}", false);

        [UserSetting("General Settings", "Enable nullable reference type checking", "Add '#nullable enable' at the top of each '.cs' file.")]
        private static readonly GeneratorSettingsValue<bool> EnableNullableReferenceTypesSetting = new(
            $"general.{nameof(EnableNullableReferenceTypes)}",
            false);

        /// <summary>
        ///     Gets or sets a value indicating whether this generator is enabled.
        ///     If disabled Unity will use the 'normal' solution generator.
        /// </summary>
        public static bool IsEnabled
        {
            get => IsEnabledSetting.value;
            set => IsEnabledSetting.value = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to generate SDK-Style projects instead of Legacy.
        /// </summary>
        public static bool GenerateSdkStyleProjects
        {
            get => GenerateSdkStyleProjectsSetting.value;
            set => GenerateSdkStyleProjectsSetting.value = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to log verbose messages produced by the solution generator.
        /// </summary>
        public static bool LogVerbose
        {
            get => LogVerboseSetting.value;
            set => LogVerboseSetting.value = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to add '#nullable enable' at the top of each '.cs' file.
        ///     <see href="https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references" />.
        /// </summary>
        public static bool EnableNullableReferenceTypes
        {
            get => EnableNullableReferenceTypesSetting.value;
            set => EnableNullableReferenceTypesSetting.value = value;
        }

        /// <summary>
        ///     Gets a list of file patterns to exclude form SDK-Style projects.
        /// </summary>
        public static IList<string> SdkExcludedFilePatterns => SdkExcludedFilePatternsSetting.value;

        /// <summary>
        ///     Gets the setting that stores the <see cref="SdkExcludedFilePatterns" /> value.
        /// </summary>
        public static GeneratorSettingsValue<List<string>> SdkExcludedFilePatternsSetting { get; } = new(
            $"sdk-style.{nameof(SdkExcludedFilePatterns)}",
            new List<string> { "**/*.meta", "**/*.asset", "**/*.prefab" });

        /// <summary>
        ///     Gets a list of additional project properties (PropertyGroup's) that should be included into SDK-Style projects.
        /// </summary>
        /// <remarks>See https://learn.microsoft.com/de-de/dotnet/core/project-sdk/msbuild-props.</remarks>
        public static IList<PropertyGroupSetting> SdkAdditionalProperties => SdkAdditionalPropertiesSetting.value;

        /// <summary>
        ///     Gets the setting that stores the <see cref="SdkAdditionalProperties" /> value.
        /// </summary>
        public static GeneratorSettingsValue<List<PropertyGroupSetting>> SdkAdditionalPropertiesSetting { get; } = new(
            $"sdk-style.{nameof(SdkAdditionalProperties)}",
            new List<PropertyGroupSetting>
            {
                new("EnableNETAnalyzers", "true"), new("AnalysisLevel", "latest"), new("AnalysisMode", "AllEnabledByDefault"),
            });

        /// <summary>
        ///     Gets a list of patterns that are used to excluded code analyzers from the .csproj file.
        /// </summary>
        /// <remarks>
        ///     This can contain '*' (wildcard). We currently use it to prevent warnings that occur in Visual Studio when a analyzer is not correctly
        ///     loadable.
        /// </remarks>
        public static IList<string> ExcludedAnalyzers => ExcludedAnalyzersSetting.value;

        /// <summary>
        ///     Gets the setting that stores the <see cref="ExcludedAnalyzers" /> value.
        /// </summary>
        public static GeneratorSettingsValue<List<string>> ExcludedAnalyzersSetting { get; } = new(
            $"legacy-style.{nameof(ExcludedAnalyzers)}",
            new List<string> { "*/Unity.SourceGenerators.dll" });
    }
}
