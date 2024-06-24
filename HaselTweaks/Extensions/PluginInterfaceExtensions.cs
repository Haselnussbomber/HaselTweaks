using System.IO;
using Dalamud.Plugin;

namespace HaselTweaks.Extensions;

public static class PluginInterfaceExtensions
{
    // This is kinda stupid, but the easiest way to call this in the config loader
    public static string GetPortraitThumbnailPath(this DalamudPluginInterface pluginInterface, Guid id)
    {
        var portraitsPath = Path.Join(pluginInterface.ConfigDirectory.FullName, "Portraits");

        if (!Directory.Exists(portraitsPath))
            Directory.CreateDirectory(portraitsPath);

        return Path.Join(portraitsPath, $"{id.ToString("D").ToLowerInvariant()}.png");
    }
}
