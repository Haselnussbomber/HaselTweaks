using System.IO;

namespace HaselTweaks.Extensions;

public static class IPluginInterfaceExtensions
{
    // This is kinda stupid, but the easiest way to call this in the config loader
    public static string GetPortraitThumbnailPath(this IDalamudPluginInterface pluginInterface, Guid id)
    {
        var portraitsPath = Path.Join(pluginInterface.ConfigDirectory.FullName, "Portraits");

        if (!Directory.Exists(portraitsPath))
            Directory.CreateDirectory(portraitsPath);

        return Path.Join(portraitsPath, $"{id.ToString("D").ToLowerInvariant()}.png");
    }
}
