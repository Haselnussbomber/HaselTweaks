namespace HaselTweaks;

public enum ConfigFieldTypes
{
    Auto,
    Ignore,
    SingleSelect,
    Color4
}

[AttributeUsage(AttributeTargets.Field)]
public class ConfigFieldAttribute : Attribute
{
    public ConfigFieldTypes Type = ConfigFieldTypes.Auto;
    public string Label = "";
    public string Description = "";
    public string OnChange = "";
    public object? DefaultValue = null!;
    public string DependsOn = ""; // MUST be a bool field

    // SingleSelect
    public string Options = "";

    // float
    public float Min = 0;
    public float Max = 100;
}
