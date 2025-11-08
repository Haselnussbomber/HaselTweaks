namespace HaselTweaks.Tweaks;

public class EnhancedIsleworksAgendaConfiguration
{
    public bool EnableSearchBar = true;
    public bool DisableTreeListTooltips = true;

    public ClientLanguage SearchLanguage = ClientLanguage.English;
}

public partial class EnhancedIsleworksAgenda
{
    public override void OnConfigChange(string fieldName)
    {
        if (Status == TweakStatus.Enabled && fieldName == "EnableSearchBar")
        {
            if (_config.EnableSearchBar && IsAddonOpen("MJICraftScheduleSetting"u8))
                _window.Open();
            else
                _window.Close();
        }
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EnableSearchBar", ref _config.EnableSearchBar);
        _configGui.DrawBool("DisableTreeListTooltips", ref _config.DisableTreeListTooltips);
    }
}
