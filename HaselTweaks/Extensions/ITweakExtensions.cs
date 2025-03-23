using HaselTweaks.Interfaces;

namespace HaselTweaks.Extensions;

public static class ITweakExtensions
{
    public static string GetInternalName(this ITweak tweak)
        => tweak.GetType().Name;
}
