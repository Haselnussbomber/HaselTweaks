using HaselTweaks.Records.PortraitHelper;
using Newtonsoft.Json;

namespace HaselTweaks.JsonConverters;

public class PortraitPresetConverter : JsonConverter<PortraitPreset>
{
    public override void WriteJson(JsonWriter writer, PortraitPreset? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.Serialize());
    }

    public override PortraitPreset ReadJson(JsonReader reader, Type objectType, PortraitPreset? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return reader.Value == null
            ? new()
            : PortraitPreset.Deserialize((string)reader.Value);
    }
}
