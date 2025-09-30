namespace HaselTweaks.Tweaks;

[AutoConstruct]
public abstract unsafe partial class ConfigurableTweak<T> : Tweak, IConfigurableTweak
{
    protected readonly ConfigGui _configGui;
    protected readonly T _config;

    public virtual void DrawConfig() { }

    public virtual void OnConfigChange(string fieldName) { }

    public virtual void OnConfigClose() { }

    public virtual void OnConfigOpen() { }
}
