namespace HaselTweaks.Extensions;

public static class SeStringBuilderExtensions
{
    public static SeStringBuilder AppendHaselTweaksPrefix(this SeStringBuilder sb)
    {
        return sb.PushColorType(32).Append("\uE078 ").PopColorType();
    }
}
