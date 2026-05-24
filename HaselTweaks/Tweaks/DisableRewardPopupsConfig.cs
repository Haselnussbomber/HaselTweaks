namespace HaselTweaks.Tweaks;

public class DisableRewardPopupsConfiguration
{
    public bool DisableFateReward = false;
    public bool DisableGoldSaucerReward = false;
    public bool DisableWKSReward = false;
}

public partial class DisableRewardPopups
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("DisableFateReward", ref _config.DisableFateReward);
        _configGui.DrawBool("DisableGoldSaucerReward", ref _config.DisableGoldSaucerReward);
        _configGui.DrawBool("DisableWKSReward", ref _config.DisableWKSReward);
    }
}
