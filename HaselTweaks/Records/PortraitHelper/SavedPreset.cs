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

    [JsonConstructor]
    public SavedPreset(Guid Id, string Name, PortraitPreset? Preset, HashSet<Guid> Tags)
    {
        this.Id = Id;
        this.Name = Name;
        this.Preset = Preset;
        this.Tags = Tags;
    }

    public void Delete()
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
                Service.PluginLog.Error(ex, $"Could not delete \"{thumbPath}\"");
            }
        }

        Service.GetService<Configuration>().Tweaks.PortraitHelper.Presets.Remove(this);
        Service.GetService<Configuration>().Save();
    }
}
