namespace HaselTweaks.Tweaks;

public class InventoryHighlightConfiguration
{
    public bool IgnoreQuality = true;
}

public partial class InventoryHighlight
{
    private InventoryHighlightConfiguration Config => _pluginConfig.Tweaks.InventoryHighlight;

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreQuality", ref Config.IgnoreQuality);
    }
}
