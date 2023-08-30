using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    public class GeneratorSettingsValue<T> : UserSetting<T>
    {
        public GeneratorSettingsValue(string key, T value)
            : base(GeneratorSettingsManager.Instance, key, value)
        {
        }
    }
}
