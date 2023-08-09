using HaselTweaks.Enums;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Class)]
public class TweakAttribute : Attribute
{
    public TweakAttribute(string Name)
    {
        this.Name = Name;
    }

    public TweakAttribute(string Name, string Description) : this(Name)
    {
        this.Description = Description;
    }

    public TweakAttribute(string Name, string Description, TweakFlags Flags) : this(Name, Description)
    {
        this.Flags = Flags;
    }

    public string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public TweakFlags Flags { get; init; } = TweakFlags.None;
}
