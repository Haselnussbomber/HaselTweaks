using System.Text.Json;
using System.Text.Json.Serialization;
using HaselTweaks.Records.PortraitHelper;

namespace HaselTweaks.JsonConverters;

public class PortraitPresetConverter : JsonConverter<PortraitPreset>
{
    public override void Write(Utf8JsonWriter writer, PortraitPreset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value?.ToExportedString());

    public override PortraitPreset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => PortraitPreset.FromExportedString(reader.GetString());
}
