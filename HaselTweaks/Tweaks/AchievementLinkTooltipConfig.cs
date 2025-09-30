namespace HaselTweaks.Tweaks;

public class AchievementLinkTooltipConfiguration
{
    public bool ShowCompletionStatus = true;
    public bool PreventSpoiler = true;
}

public partial class AchievementLinkTooltip
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("ShowCompletionStatus", ref _config.ShowCompletionStatus);
        _configGui.DrawBool("PreventSpoiler", ref _config.PreventSpoiler, noFixSpaceAfter: true);
    }
}
