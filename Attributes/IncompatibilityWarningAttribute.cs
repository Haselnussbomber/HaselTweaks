using System.Linq;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IncompatibilityWarning : Attribute
{
    public IncompatibilityWarning(
        string InternalName,
        string Name,
        params string[] ConfigNames)
    {
        this.InternalName = InternalName;
        this.Name = Name;
        this.ConfigNames = ConfigNames;
    }

    public string InternalName { get; }
    public string Name { get; }
    public string[] ConfigNames { get; }

    public bool IsLoaded
        => Service.PluginInterface.InstalledPlugins.Any(p => p.InternalName == InternalName && p.IsLoaded);
}
