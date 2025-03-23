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
    private EnhancedIsleworksAgendaConfiguration Config => _pluginConfig.Tweaks.EnhancedIsleworksAgenda;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (fieldName == "EnableSearchBar")
        {
            if (Config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"))
                _window.Open();
            else
                _window.Close();
        }
    }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EnableSearchBar", ref Config.EnableSearchBar);
        _configGui.DrawBool("DisableTreeListTooltips", ref Config.DisableTreeListTooltips);
    }
}
