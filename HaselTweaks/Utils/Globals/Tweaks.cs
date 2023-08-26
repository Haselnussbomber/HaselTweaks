using System.Linq;

namespace HaselTweaks.Utils.Globals;

public static class Tweaks
{
    public static T GetTweak<T>() where T : Tweak
        => Plugin.Tweaks.OfType<T>().First();
}
