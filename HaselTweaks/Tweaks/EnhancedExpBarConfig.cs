namespace HaselTweaks.Tweaks;

public class EnhancedExpBarConfiguration
{
    public bool ForcePvPSeriesBar = true;
    public bool ForceSanctuaryBar = true;
    public bool ForceCompanionBar = true;
    public bool ForceCosmicResearchBar = true;
    public bool ShowCosmicToolScore = true;
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
    // No CosmicResearchBar, because same reason as above
}

public unsafe partial class EnhancedExpBar
{
    private EnhancedExpBarConfiguration Config => _pluginConfig.Tweaks.EnhancedExpBar;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (Status == TweakStatus.Enabled)
            TriggerReset();
    }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["ShowExperiencePercentage"])]);

        _configGui.DrawConfigurationHeader();

        _configGui.DrawBool("ForcePvPSeriesBar", ref Config.ForcePvPSeriesBar);
        _configGui.DrawBool("ForceSanctuaryBar", ref Config.ForceSanctuaryBar);
        _configGui.DrawBool("ForceCompanionBar", ref Config.ForceCompanionBar);
        _configGui.DrawBool("ForceCosmicResearchBar", ref Config.ForceCosmicResearchBar, drawAfterDescription: () =>
        {
            _configGui.DrawBool("ShowCosmicToolScore", ref Config.ShowCosmicToolScore);
        });
        _configGui.DrawBool("SanctuaryBarHideJob", ref Config.SanctuaryBarHideJob);
        _configGui.DrawEnum("MaxLevelOverride", ref Config.MaxLevelOverride);
        _configGui.DrawBool("DisableColorChanges", ref Config.DisableColorChanges);
    }
}
