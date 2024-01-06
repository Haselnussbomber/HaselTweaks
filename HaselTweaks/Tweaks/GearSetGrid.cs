using HaselCommon.Services;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public class GearSetGridConfiguration
{
    [BoolConfig]
    public bool AutoOpenWithGearSetList = false;

    [BoolConfig]
    public bool RegisterCommand = true;

    [BoolConfig]
    public bool ConvertSeparators = true;

    [StringConfig(DependsOn = nameof(ConvertSeparators), DefaultValue = "===========")]
    public string SeparatorFilter = "===========";

    [BoolConfig(DependsOn = nameof(ConvertSeparators))]
    public bool DisableSeparatorSpacing = false;
}

[Tweak]
public class GearSetGrid : Tweak<GearSetGridConfiguration>
{
    public override void Enable()
    {
        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            Service.WindowManager.OpenWindow<GearSetGridWindow>();
    }

    public override void Disable()
    {
        if (Service.HasService<WindowManager>())
            Service.WindowManager.CloseWindow<GearSetGridWindow>();
    }

    public override void OnAddonOpen(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Service.WindowManager.OpenWindow<GearSetGridWindow>();
    }

    public override void OnAddonClose(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Service.WindowManager.CloseWindow<GearSetGridWindow>();
    }

    [CommandHandler("/gsg", "GearSetGrid.CommandHandlerHelpMessage", nameof(Config.RegisterCommand))]
    private void OnGsgCommand(string command, string arguments)
    {
        Service.WindowManager.ToggleWindow<GearSetGridWindow>();
    }
}
