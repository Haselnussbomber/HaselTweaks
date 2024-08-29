namespace HaselTweaks.Tweaks;

public class InventoryHighlightConfiguration
{
    public bool IgnoreQuality = true;
}

public partial class InventoryHighlight
{
    private InventoryHighlightConfiguration Config => PluginConfig.Tweaks.InventoryHighlight;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("IgnoreQuality", ref Config.IgnoreQuality);
    }
}
