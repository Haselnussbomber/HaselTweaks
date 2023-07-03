using System.Reflection;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Records;

internal record ConfigDrawData<T> : IConfigDrawData
{
    public Tweak Tweak { get; init; } = null!;

    public object Config { get; init; } = null!;
    public FieldInfo Field { get; init; } = null!;
    public ConfigFieldAttribute? Attr { get; init; }

    public string Key => $"###{Tweak.InternalName}#{Field.Name}";
    public string Label => Attr != null && !string.IsNullOrEmpty(Attr.Label) ? Attr.Label : Field.Name;
    public string Description => Attr?.Description ?? string.Empty;

    public T? Value
    {
        get => (T?)Field.GetValue(Config);
        set
        {
            Field.SetValue(Config, value);
            OnChange();
            Plugin.Config.Save();
        }
    }

    public void OnChange()
    {
        if (Attr == null || string.IsNullOrEmpty(Attr.OnChange)) return;

        var method = Tweak.CachedType.GetMethod(Attr.OnChange, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return;

        method.Invoke(Tweak, null); // TODO: add event parameters
    }
}
