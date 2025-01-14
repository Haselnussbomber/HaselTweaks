using HaselCommon.Commands;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public partial class GearSetGrid(
    PluginConfig pluginConfig,
    ConfigGui ConfigGui,
    CommandService CommandService,
    AddonObserver AddonObserver,
    GearSetGridWindow Window)
    : IConfigurableTweak
{
    public string InternalName => nameof(GearSetGrid);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private CommandHandler? GsgCommand;

    public void OnInitialize()
    {
        GsgCommand = CommandService.Register(OnGsgCommand);
    }

    public void OnEnable()
    {
        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;

        GsgCommand?.SetEnabled(Config.RegisterCommand);

        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            Window.Open();
    }

    public void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;

        GsgCommand?.SetEnabled(false);

        Window.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        GsgCommand?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void OnAddonOpen(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Window.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            Window.Close();
    }

    [CommandHandler("/gsg", "GearSetGrid.CommandHandlerHelpMessage")]
    private void OnGsgCommand(string command, string arguments)
    {
        if (Window.IsOpen)
            Window.Close();
        else
            Window.Open();
    }
}
