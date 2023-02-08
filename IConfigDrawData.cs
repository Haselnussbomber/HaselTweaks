using System.Reflection;

namespace HaselTweaks;

public interface IConfigDrawData
{
    public Tweak Tweak { get; init; }

    public object Config { get; init; }
    public FieldInfo Field { get; init; }
    public ConfigFieldAttribute? Attr { get; init; }

    public string Key { get; }
    public string Label { get; }
    public string Description { get; }
}
