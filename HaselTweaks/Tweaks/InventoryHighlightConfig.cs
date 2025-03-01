namespace HaselTweaks.Tweaks;

public class InventoryHighlightConfiguration
{
    public bool IgnoreQuality = true;
}

public partial class InventoryHighlight
{
    private InventoryHighlightConfiguration Config => _pluginConfig.Tweaks.InventoryHighlight;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreQuality", ref Config.IgnoreQuality);
    }
}
