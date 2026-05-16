namespace HaselTweaks.Tweaks;

public class GlamourDresserAlertConfiguration
{
    public bool IgnoreOutfits = false;
    public bool IgnoreDyedGlamour = false;
    public bool IgnoreDuplicates = false;
}

public partial class GlamourDresserAlert
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreOutfits", ref _config.IgnoreOutfits);
        _configGui.DrawBool("IgnoreDyedGlamour", ref _config.IgnoreDyedGlamour);
        _configGui.DrawBool("IgnoreDuplicates", ref _config.IgnoreDuplicates);
    }

    public override void OnConfigChange(string fieldName)
    {
        _lastItemIds = [];
    }
}
