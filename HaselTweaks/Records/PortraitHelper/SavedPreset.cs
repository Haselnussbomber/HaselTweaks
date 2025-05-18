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
}
