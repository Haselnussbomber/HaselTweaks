using HaselTweaks.Config;

namespace HaselTweaks.Tweaks;

public class AetherCurrentHelperConfiguration
{
    public bool AlwaysShowDistance = false;
    public bool CenterDistance = true;
}

public partial class AetherCurrentHelper
{
    private AetherCurrentHelperConfiguration Config => PluginConfig.Tweaks.AetherCurrentHelper;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("AlwaysShowDistance", ref Config.AlwaysShowDistance);
        ConfigGui.DrawBool("CenterDistance", ref Config.CenterDistance, noFixSpaceAfter: true);
    }
}
