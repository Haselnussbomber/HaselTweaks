namespace HaselTweaks.Tweaks;

public class EnhancedTargetInfoConfiguration
{
    public bool DisplayMountStatus = false;
    public bool DisplayOrnamentStatus = false;
    public bool RemoveLeadingZeroInHPPercentage = false;
}

public unsafe partial class EnhancedTargetInfo
{
    private EnhancedTargetInfoConfiguration Config => _pluginConfig.Tweaks.EnhancedTargetInfo;

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("DisplayMountStatus", ref Config.DisplayMountStatus);
        _configGui.DrawBool("DisplayOrnamentStatus", ref Config.DisplayOrnamentStatus);
        _configGui.DrawBool("RemoveLeadingZeroInHPPercentage", ref Config.RemoveLeadingZeroInHPPercentage);
    }
}
