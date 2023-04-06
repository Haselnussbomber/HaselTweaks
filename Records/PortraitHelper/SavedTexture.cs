using HaselTweaks.JsonConverters;
using Newtonsoft.Json;

namespace HaselTweaks.Records.PortraitHelper;

public record SavedTexture
{
    [JsonConverter(typeof(ByteArrayConverter))]
    public byte[] Data;
    public int Width;
    public int Height;
    public string Hash;

    [JsonConstructor]
    public SavedTexture(byte[] Data, int Width, int Height, string Hash)
    {
        this.Data = Data;
        this.Width = Width;
        this.Height = Height;
        this.Hash = Hash;
    }

    public SavedTexture()
    {
        Data = Array.Empty<byte>();
        Width = 0;
        Height = 0;
        Hash = string.Empty;
    }
}
