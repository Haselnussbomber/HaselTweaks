using System.Collections.Generic;
using Newtonsoft.Json;

namespace HaselTweaks.Records.PortraitHelper;

public record SavedPreset
{
    public Guid Id;
    public string Name;
    public PortraitPreset Preset;
    public List<Guid> Tags;
    public SavedTexture Texture;

    [JsonConstructor]
    public SavedPreset(Guid Id, string Name, PortraitPreset Preset, List<Guid> Tags, SavedTexture Texture)
    {
        this.Id = Id;
        this.Name = Name;
        this.Preset = Preset;
        this.Tags = Tags;
        this.Texture = Texture;
    }

    public SavedPreset(string Name, PortraitPreset Preset) : this(Guid.NewGuid(), Name, Preset, new(), new())
    {
    }

    public SavedPreset(string Name, PortraitPreset Preset, List<Guid> Tags, SavedTexture Texture) : this(Guid.NewGuid(), Name, Preset, Tags, Texture)
    {
    }
}
