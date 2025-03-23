namespace HaselTweaks.Tweaks;

public class AchievementLinkTooltipConfiguration
{
    public bool ShowCompletionStatus = true;
    public bool PreventSpoiler = true;
}

public partial class AchievementLinkTooltip
{
    private AchievementLinkTooltipConfiguration Config => _pluginConfig.Tweaks.AchievementLinkTooltip;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("ShowCompletionStatus", ref Config.ShowCompletionStatus);
        _configGui.DrawBool("PreventSpoiler", ref Config.PreventSpoiler, noFixSpaceAfter: true);
    }
}
