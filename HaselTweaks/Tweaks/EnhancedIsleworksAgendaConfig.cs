using Dalamud.Game;

namespace HaselTweaks.Tweaks;

public class EnhancedIsleworksAgendaConfiguration
{
    public bool EnableSearchBar = true;
    public bool DisableTreeListTooltips = true;

    public ClientLanguage SearchLanguage = ClientLanguage.English;
}

public partial class EnhancedIsleworksAgenda
{
    private EnhancedIsleworksAgendaConfiguration Config => PluginConfig.Tweaks.EnhancedIsleworksAgenda;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (fieldName == "EnableSearchBar")
        {
            if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
                Window.Open();
            else
                Window.Close();
        }
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("EnableSearchBar", ref Config.EnableSearchBar);
        ConfigGui.DrawBool("DisableTreeListTooltips", ref Config.DisableTreeListTooltips);
    }
}
