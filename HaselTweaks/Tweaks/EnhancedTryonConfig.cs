namespace HaselTweaks.Tweaks;

public class EnhancedTryonConfiguration
{
    public bool KeepFacewearOn = true;
}

public partial class EnhancedTryon
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("KeepFacewearOn", ref _config.KeepFacewearOn);
    }
}
