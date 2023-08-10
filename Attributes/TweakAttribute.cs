using HaselTweaks.Enums;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Class)]
public class TweakAttribute : Attribute
{
    public TweakAttribute()
    {
    }

    public TweakAttribute(TweakFlags Flags) 
    {
        this.Flags = Flags;
    }

    public TweakFlags Flags { get; init; } = TweakFlags.None;
}
