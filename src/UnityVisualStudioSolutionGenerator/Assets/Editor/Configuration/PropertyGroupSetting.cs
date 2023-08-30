using System;
using UnityEngine;

namespace UnityVisualStudioSolutionGenerator
{
    [Serializable]
    public class PropertyGroupSetting
    {
        public PropertyGroupSetting(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [field: SerializeField]
        public string Name { get; set; }

        [field: SerializeField]
        public string Value { get; set; }
    }
}
