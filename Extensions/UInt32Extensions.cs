namespace HaselTweaks.Extensions;

public static class UInt32Extensions
{
    public static uint Reverse(this uint value)
        => (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
            (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
}
