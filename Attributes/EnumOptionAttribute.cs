using System;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class EnumOptionAttribute : Attribute
{
    public string Label = "";

    public EnumOptionAttribute(string label)
    {
        Label = label;
    }
}
