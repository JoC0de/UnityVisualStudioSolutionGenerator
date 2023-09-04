using System;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    /// <summary>
    ///     Data container for a PropertyGroup that should be included in the generated .csproj file.
    /// </summary>
    [Serializable]
    public class PropertyGroupSetting
    {
        /// <summary>
        ///     Gets or sets the name of the property.
        /// </summary>
        [field: SerializeField]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the property value.
        /// </summary>
        [field: SerializeField]
        public string Value { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PropertyGroupSetting" /> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The property value.</param>
        public PropertyGroupSetting(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
