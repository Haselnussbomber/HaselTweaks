using System.Text.Json;
using System.Text.Json.Serialization;
using Lumina.Text.ReadOnly;

namespace HaselTweaks.JsonConverters;

public class ReadOnlySeStringConverter : JsonConverter<ReadOnlySeString>
{
    public override void Write(Utf8JsonWriter writer, ReadOnlySeString value, JsonSerializerOptions options)
        => writer.WriteBase64StringValue(value);

    public override ReadOnlySeString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetBytesFromBase64());
}
