namespace HaselTweaks.Tweaks;

public class AetherCurrentHelperConfiguration
{
    public bool AlwaysShowDistance = false;
    public bool CenterDistance = true;
}

public partial class AetherCurrentHelper
{
    private AetherCurrentHelperConfiguration Config => _pluginConfig.Tweaks.AetherCurrentHelper;

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("AlwaysShowDistance", ref Config.AlwaysShowDistance);
        _configGui.DrawBool("CenterDistance", ref Config.CenterDistance, noFixSpaceAfter: true);
    }
}
