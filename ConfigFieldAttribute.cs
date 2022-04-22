using System;

namespace HaselTweaks
{
    public enum ConfigFieldTypes
    {
        Auto,
        SingleSelect
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigFieldAttribute : Attribute
    {
        public ConfigFieldTypes Type = ConfigFieldTypes.Auto;
        public string Options;
        public string Label;
        public string Description;
    }
}
