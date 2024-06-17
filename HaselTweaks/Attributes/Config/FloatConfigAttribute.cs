namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class FloatConfigAttribute : BaseConfigAttribute
{
    public float DefaultValue = 0;
    public float Min = 0;
    public float Max = 100;
}
