namespace HaselTweaks.Tweaks;

[AutoConstruct]
public abstract unsafe partial class ConfigurableTweak : Tweak, IConfigurableTweak
{
    public virtual void DrawConfig() { }

    public virtual void OnConfigChange(string fieldName) { }

    public virtual void OnConfigClose() { }

    public virtual void OnConfigOpen() { }
}
