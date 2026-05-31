namespace HaselTweaks.Tweaks;

public class FlashTaskbarConfiguration
{
    public bool FlashOnAlarm;
    public bool FlashOnCombat;
    public bool FlashOnCraftEnd;
}

public partial class FlashTaskbar
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("FlashOnAlarm", ref _config.FlashOnAlarm);
        _configGui.DrawBool("FlashOnCombat", ref _config.FlashOnCombat);
        _configGui.DrawBool("FlashOnCraftEnd", ref _config.FlashOnCraftEnd);
    }
}
