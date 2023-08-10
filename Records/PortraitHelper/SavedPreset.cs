using System.Collections.Generic;
using System.IO;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace HaselTweaks.Records.PortraitHelper;

public record SavedPreset
{
    public Guid Id;
    public string Name;
    public PortraitPreset? Preset;
    public HashSet<Guid> Tags;
    public string TextureHash;

    [JsonConstructor]
    public SavedPreset(Guid Id, string Name, PortraitPreset? Preset, HashSet<Guid> Tags, string TextureHash)
    {
        this.Id = Id;
        this.Name = Name;
        this.Preset = Preset;
        this.Tags = Tags;
        this.TextureHash = TextureHash;
    }

    public SavedPreset(string Name, PortraitPreset? Preset) : this(Guid.NewGuid(), Name, Preset, new(), string.Empty)
    {
    }

    public SavedPreset(string Name, PortraitPreset? Preset, HashSet<Guid> Tags, string TextureHash) : this(Guid.NewGuid(), Name, Preset, Tags, TextureHash)
    {
    }

    public void Delete()
    {
        var config = Plugin.Config.Tweaks.PortraitHelper;

        var thumbPath = Tweaks.PortraitHelper.Configuration.GetPortraitThumbnailPath(TextureHash);
        if (File.Exists(thumbPath))
        {
            try
            {
                File.Delete(thumbPath);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Could not delete \"{thumbPath}\"");
            }
        }

        config.Presets.Remove(this);
        Plugin.Config.Save();
    }
}
