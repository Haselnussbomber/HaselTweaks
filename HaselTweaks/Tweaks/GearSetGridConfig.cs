using Dalamud.Interface.Utility.Raii;
using HaselTweaks.Config;

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
    private GearSetGridConfiguration Config => pluginConfig.Tweaks.GearSetGrid;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (fieldName == "RegisterCommand")
        {
            GsgCommand?.SetEnabled(Config.RegisterCommand);
        }
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("AutoOpenWithGearSetList", ref Config.AutoOpenWithGearSetList);
        ConfigGui.DrawBool("RegisterCommand", ref Config.RegisterCommand);
        ConfigGui.DrawBool("ConvertSeparators", ref Config.ConvertSeparators, drawAfterDescription: () =>
        {
            using var disabled = ImRaii.Disabled(!Config.ConvertSeparators);

            ConfigGui.DrawString("SeparatorFilter", ref Config.SeparatorFilter, defaultValue: "===========");
            ConfigGui.DrawBool("DisableSeparatorSpacing", ref Config.DisableSeparatorSpacing);
        });
    }
}
