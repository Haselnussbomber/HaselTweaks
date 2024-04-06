using System.Text.Json;
using System.Text.Json.Serialization;
using HaselCommon.Text;

namespace HaselTweaks.JsonConverters;

public class HaselCommonTextSeStringConverter : JsonConverter<SeString>
{
    public override void Write(Utf8JsonWriter writer, SeString value, JsonSerializerOptions options)
        => writer.WriteBase64StringValue(value.Encode());

    public override SeString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => SeString.Parse(reader.GetBytesFromBase64());
}
