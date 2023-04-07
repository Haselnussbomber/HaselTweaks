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
    public List<Guid> Tags;
    public string TextureHash;

    [JsonConstructor]
    public SavedPreset(Guid Id, string Name, PortraitPreset? Preset, List<Guid> Tags, string TextureHash)
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

    public SavedPreset(string Name, PortraitPreset? Preset, List<Guid> Tags, string TextureHash) : this(Guid.NewGuid(), Name, Preset, Tags, TextureHash)
    {
    }

    public void Delete()
    {
        var thumbPath = Plugin.Config.GetPortraitThumbnailPath(TextureHash);
        if (File.Exists(thumbPath))
        {
            try
            {
                File.Delete(thumbPath);
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not delete \"{thumbPath}\"", e);
            }
        }

        Plugin.Config.Tweaks.PortraitHelper.Presets.Remove(this);
        Plugin.Config.Save();
    }
}
