using System;

namespace HaselTweaks
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigFieldAttribute : Attribute
    {
        public string Label;
        public string Description;
    }
}
