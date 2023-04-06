using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;

namespace HaselTweaks.JsonConverters;

public class ByteArrayConverter : JsonConverter<byte[]>
{
    public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        using var outputStream = new MemoryStream();
        using var compressionStream = new GZipStream(outputStream, CompressionLevel.SmallestSize);
        compressionStream.Write(value);
        writer.WriteValue(Convert.ToBase64String(outputStream.ToArray()));
    }

    public override byte[] ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.Value == null)
            return Array.Empty<byte>();

        var data = Convert.FromBase64String((string)reader.Value);

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
        decompressionStream.CopyTo(outputStream);

        return outputStream.ToArray();
    }
}
