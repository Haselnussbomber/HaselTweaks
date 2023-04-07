using HaselTweaks.Records.PortraitHelper;
using Newtonsoft.Json;

namespace HaselTweaks.JsonConverters;

public class PortraitPresetConverter : JsonConverter<PortraitPreset>
{
    public override void WriteJson(JsonWriter writer, PortraitPreset? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToExportedString());
    }

    public override PortraitPreset? ReadJson(JsonReader reader, Type objectType, PortraitPreset? existingValue, bool hasExistingValue, JsonSerializer serializer)
        => PortraitPreset.FromExportedString((string?)reader.Value);
}
