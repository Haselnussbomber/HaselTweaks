namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class StringConfigAttribute : BaseConfigAttribute
{
    public string DefaultValue = string.Empty;
}
