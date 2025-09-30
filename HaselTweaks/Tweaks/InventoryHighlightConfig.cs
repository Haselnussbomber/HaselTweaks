namespace HaselTweaks.Tweaks;

public class InventoryHighlightConfiguration
{
    public bool IgnoreQuality = true;
}

public partial class InventoryHighlight
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreQuality", ref _config.IgnoreQuality);
    }
}
