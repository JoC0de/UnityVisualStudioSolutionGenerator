using UnityEditor.SettingsManagement;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     A setting value that is stored in the <see cref="GeneratorSettingsManager.Instance" />.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the setting.</typeparam>
    public class GeneratorSettingsValue<T> : UserSetting<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GeneratorSettingsValue{T}" /> class.
        /// </summary>
        /// <param name="key">The name of the setting value.</param>
        /// <param name="value">The current / initial value of the setting.</param>
        public GeneratorSettingsValue(string key, T value)
            : base(GeneratorSettingsManager.Instance, key, value)
        {
        }
    }
}
