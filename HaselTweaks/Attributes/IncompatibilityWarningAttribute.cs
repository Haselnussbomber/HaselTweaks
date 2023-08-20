using System.Linq;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IncompatibilityWarningAttribute : Attribute
{
    public IncompatibilityWarningAttribute(
        string InternalName,
        params string[] ConfigNames)
    {
        this.InternalName = InternalName;
        this.ConfigNames = ConfigNames;
    }

    public string InternalName { get; }
    public string[] ConfigNames { get; }

    public bool IsLoaded
        => Service.PluginInterface.InstalledPlugins.Any(p => p.InternalName == InternalName && p.IsLoaded);
}
