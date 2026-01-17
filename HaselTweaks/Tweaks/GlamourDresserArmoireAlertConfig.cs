namespace HaselTweaks.Tweaks;

public class GlamourDresserArmoireAlertConfiguration
{
    public bool IgnoreOutfits = false;
}

public unsafe partial class GlamourDresserArmoireAlert
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreOutfits", ref _config.IgnoreOutfits);
    }
}
