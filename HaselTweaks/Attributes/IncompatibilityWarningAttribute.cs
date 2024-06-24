namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class IncompatibilityWarningAttribute(string InternalName, params string[] ConfigNames) : Attribute
{
    public string InternalName { get; } = InternalName;
    public string[] ConfigNames { get; } = ConfigNames;
}
