namespace HaselTweaks;

public enum ConfigFieldTypes
{
    Auto,
    SingleSelect
}

[AttributeUsage(AttributeTargets.Field)]
public class ConfigFieldAttribute : Attribute
{
    public ConfigFieldTypes Type = ConfigFieldTypes.Auto;
    public string Label = "";
    public string Description = "";
    public string OnChange = "";
    public object? DefaultValue = null!;

    // SingleSelect
    public string Options = "";

    // float
    public float Min = 0;
    public float Max = 100;
}
