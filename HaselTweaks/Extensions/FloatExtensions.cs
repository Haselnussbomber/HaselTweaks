namespace HaselTweaks.Extensions;

internal static class FloatExtensions
{
    public static bool IsApproximately(this float a, float b, float margin = 0.001f)
        => Math.Abs(a - b) < margin;
}
