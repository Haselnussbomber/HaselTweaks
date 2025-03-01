namespace HaselTweaks.Tweaks;

public class AetherCurrentHelperConfiguration
{
    public bool AlwaysShowDistance = false;
    public bool CenterDistance = true;
}

public partial class AetherCurrentHelper
{
    private AetherCurrentHelperConfiguration Config => _pluginConfig.Tweaks.AetherCurrentHelper;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("AlwaysShowDistance", ref Config.AlwaysShowDistance);
        _configGui.DrawBool("CenterDistance", ref Config.CenterDistance, noFixSpaceAfter: true);
    }
}
