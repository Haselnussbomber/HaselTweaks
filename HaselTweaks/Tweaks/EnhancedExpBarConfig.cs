namespace HaselTweaks.Tweaks;

public class EnhancedExpBarConfiguration
{
    public bool ForcePvPSeriesBar = true;
    public bool ForceSanctuaryBar = true;
    public bool ForceCompanionBar = true;
    public bool SanctuaryBarHideJob = false;
    public MaxLevelOverrideType MaxLevelOverride = MaxLevelOverrideType.Default;
    public bool DisableColorChanges = false;
}

public enum MaxLevelOverrideType
{
    Default,
    PvPSeriesBar,
    CompanionBar,
    // No SanctuaryBar, because data is only available on the island
}

public unsafe partial class EnhancedExpBar
{
    public override void OnConfigChange(string fieldName)
    {
        if (Status == TweakStatus.Enabled)
            TriggerReset();
    }

    public override void DrawConfig()
    {
        _configGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["ShowExperiencePercentage"])]);

        _configGui.DrawConfigurationHeader();

        _configGui.DrawBool("ForcePvPSeriesBar", ref _config.ForcePvPSeriesBar);
        _configGui.DrawBool("ForceSanctuaryBar", ref _config.ForceSanctuaryBar);
        _configGui.DrawBool("ForceCompanionBar", ref _config.ForceCompanionBar);
        _configGui.DrawBool("SanctuaryBarHideJob", ref _config.SanctuaryBarHideJob);
        _configGui.DrawEnum("MaxLevelOverride", ref _config.MaxLevelOverride);
        _configGui.DrawBool("DisableColorChanges", ref _config.DisableColorChanges);
    }
}
