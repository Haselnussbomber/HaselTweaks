using System;

namespace HaselTweaks.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class EnumOptionAttribute : Attribute
{
    public string Label = "";

    public EnumOptionAttribute(string label)
    {
        Label = label;
    }
}
