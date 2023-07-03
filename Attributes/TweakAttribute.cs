namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Class)]
public class TweakAttribute : Attribute
{
    public TweakAttribute(
        string Name,
        string Description = "",
        bool HasCustomConfig = false)
    {
        this.Name = Name;
        this.Description = Description;
        this.HasCustomConfig = HasCustomConfig;
    }

    public string Name { get; }
    public string Description { get; }
    public bool HasCustomConfig { get; }
}
