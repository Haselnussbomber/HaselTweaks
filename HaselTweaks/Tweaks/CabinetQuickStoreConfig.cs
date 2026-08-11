namespace HaselTweaks.Tweaks;

public class CabinetQuickStoreConfiguration
{
    public bool IgnoreItemsInGearsets = false;
}

public partial class CabinetQuickStore
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreItemsInGearsets", ref _config.IgnoreItemsInGearsets);
    }
}
