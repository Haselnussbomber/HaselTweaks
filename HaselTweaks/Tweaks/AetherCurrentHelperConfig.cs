namespace HaselTweaks.Tweaks;

public class AetherCurrentHelperConfiguration
{
    public bool AlwaysShowDistance = false;
    public bool CenterDistance = true;
}

public partial class AetherCurrentHelper
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("AlwaysShowDistance", ref _config.AlwaysShowDistance);
        _configGui.DrawBool("CenterDistance", ref _config.CenterDistance, noFixSpaceAfter: true);
    }
}
