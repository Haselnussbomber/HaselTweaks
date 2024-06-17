namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class EnumConfigAttribute : BaseConfigAttribute
{
    public bool NoLabel = false;
}
