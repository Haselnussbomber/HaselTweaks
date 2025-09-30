namespace HaselTweaks.Tweaks;

public class GearSetGridConfiguration
{
    public bool AutoOpenWithGearSetList = false;
    public bool RegisterCommand = true;
    public bool ConvertSeparators = true;
    public string SeparatorFilter = "===========";
    public bool DisableSeparatorSpacing = false;
}

public partial class GearSetGrid
{
    public override void OnConfigChange(string fieldName)
    {
        if (Status == TweakStatus.Enabled && fieldName == "RegisterCommand")
        {
            _gsgCommand?.SetEnabled(_config.RegisterCommand);
        }
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("AutoOpenWithGearSetList", ref _config.AutoOpenWithGearSetList);
        _configGui.DrawBool("RegisterCommand", ref _config.RegisterCommand);
        _configGui.DrawBool("ConvertSeparators", ref _config.ConvertSeparators, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!_config.ConvertSeparators);

            _configGui.DrawString("SeparatorFilter", ref _config.SeparatorFilter, defaultValue: "===========");
            _configGui.DrawBool("DisableSeparatorSpacing", ref _config.DisableSeparatorSpacing);
        });
    }
}
