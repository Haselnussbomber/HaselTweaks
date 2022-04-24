using System.Reflection;

namespace HaselTweaks;

internal class ConfigDrawData<T>
{
    public Plugin Plugin { get; init; } = null!;
    public Tweak Tweak { get; init; } = null!;

    public string Key { get; init; } = null!;
    public string Label { get; init; } = null!;
    public object Config { get; init; } = null!;
    public FieldInfo Field { get; init; } = null!;
    public ConfigFieldAttribute? Attr { get; init; }

    public T? Value
    {
        get { return (T?)Field.GetValue(Config); }
        set
        {
            Field.SetValue(Config, value);
            Plugin.SaveConfig();
            OnChange();
        }
    }

    public void OnChange()
    {
        if (Attr == null || string.IsNullOrEmpty(Attr.OnChange)) return;

        var method = Tweak.GetType().GetMethod(Attr.OnChange, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return;

        method.Invoke(Tweak, null); // TODO: add event parameters
    }
}
