using HaselTweaks.Config;

namespace HaselTweaks.Tweaks;

public class AchievementLinkTooltipConfiguration
{
    public bool ShowCompletionStatus = true;
    public bool PreventSpoiler = true;
}

public partial class AchievementLinkTooltip
{
    private AchievementLinkTooltipConfiguration Config => PluginConfig.Tweaks.AchievementLinkTooltip;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("ShowCompletionStatus", ref Config.ShowCompletionStatus);
        ConfigGui.DrawBool("PreventSpoiler", ref Config.PreventSpoiler, noFixSpaceAfter: true);
    }
}
