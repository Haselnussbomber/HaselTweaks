using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

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

    public void Delete()
    {
        var config = Service.GetService<Configuration>().Tweaks.PortraitHelper;

        var thumbPath = Tweaks.PortraitHelper.GetPortraitThumbnailPath(TextureHash);
        if (File.Exists(thumbPath))
        {
            try
            {
                File.Delete(thumbPath);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, $"Could not delete \"{thumbPath}\"");
            }
        }

        config.Presets.Remove(this);
        Service.GetService<Configuration>().Save();
    }
}
