namespace HaselTweaks.Tweaks;

public class EnhancedTargetInfoConfiguration
{
    public bool DisplayMountStatus = false;
    public bool DisplayOrnamentStatus = false;
    public bool RemoveLeadingZeroInHPPercentage = false;
}

public unsafe partial class EnhancedTargetInfo
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("DisplayMountStatus", ref _config.DisplayMountStatus);
        _configGui.DrawBool("DisplayOrnamentStatus", ref _config.DisplayOrnamentStatus);
        _configGui.DrawBool("RemoveLeadingZeroInHPPercentage", ref _config.RemoveLeadingZeroInHPPercentage);
    }
}
