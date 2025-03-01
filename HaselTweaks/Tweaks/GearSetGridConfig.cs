using Dalamud.Interface.Utility.Raii;

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
    private GearSetGridConfiguration Config => _pluginConfig.Tweaks.GearSetGrid;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (fieldName == "RegisterCommand")
        {
            _gsgCommand?.SetEnabled(Config.RegisterCommand);
        }
    }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("AutoOpenWithGearSetList", ref Config.AutoOpenWithGearSetList);
        _configGui.DrawBool("RegisterCommand", ref Config.RegisterCommand);
        _configGui.DrawBool("ConvertSeparators", ref Config.ConvertSeparators, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!Config.ConvertSeparators);

            _configGui.DrawString("SeparatorFilter", ref Config.SeparatorFilter, defaultValue: "===========");
            _configGui.DrawBool("DisableSeparatorSpacing", ref Config.DisableSeparatorSpacing);
        });
    }
}
