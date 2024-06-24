namespace HaselTweaks;

public abstract class BaseConfigAttribute : Attribute
{
    public string DependsOn = string.Empty;
}
