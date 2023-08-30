#nullable enable

using System.Collections.Generic;
using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
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

        public static bool IsEnabled
        {
            get => IsEnabledSetting.value;
            set => IsEnabledSetting.value = value;
        }

        public static bool GenerateSdkStyleProjects
        {
            get => GenerateSdkStyleProjectsSetting.value;
            set => GenerateSdkStyleProjectsSetting.value = value;
        }

        public static bool LogVerbose
        {
            get => LogVerboseSetting.value;
            set => LogVerboseSetting.value = value;
        }

        public static List<string> SdkExcludedFilePatterns => SdkExcludedFilePatternsSetting.value;

        public static GeneratorSettingsValue<List<string>> SdkExcludedFilePatternsSetting { get; } = new(
            $"sdk-style.{nameof(SdkExcludedFilePatterns)}",
            new List<string> { "**/*.meta", "**/*.asset", "**/*.prefab" });

        public static List<PropertyGroupSetting> SdkAdditionalProperties => SdkAdditionalPropertiesSetting.value;

        public static GeneratorSettingsValue<List<PropertyGroupSetting>> SdkAdditionalPropertiesSetting { get; } = new(
            $"sdk-style.{nameof(SdkAdditionalProperties)}",
            new List<PropertyGroupSetting>
            {
                new("EnableNETAnalyzers", "true"), new("AnalysisLevel", "latest"), new("AnalysisMode", "AllEnabledByDefault"),
            });

        public static List<string> ExcludedAnalyzers => ExcludedAnalyzersSetting.value;

        public static GeneratorSettingsValue<List<string>> ExcludedAnalyzersSetting { get; } = new(
            $"legacy-style.{nameof(ExcludedAnalyzers)}",
            new List<string> { new("*/Unity.SourceGenerators.dll") });
    }
}
