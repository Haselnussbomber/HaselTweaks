using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Records.PortraitHelper;

public record SavedPreset
{
    public Guid Id;
    public string Name;
    public PortraitPreset? Preset;
    public HashSet<Guid> Tags;

    [JsonConstructor]
    public SavedPreset(Guid Id, string Name, PortraitPreset? Preset, HashSet<Guid> Tags)
    {
        this.Id = Id;
        this.Name = Name;
        this.Preset = Preset;
        this.Tags = Tags;
    }

    public void Delete(ILogger logger, Configuration pluginConfig)
    {
        var thumbPath = Tweaks.PortraitHelper.GetPortraitThumbnailPath(Id);
        if (File.Exists(thumbPath))
        {
            try
            {
                File.Delete(thumbPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not delete \"{thumbPath}\"");
            }
        }

        pluginConfig.Tweaks.PortraitHelper.Presets.Remove(this);
        pluginConfig.Save();
    }
}
