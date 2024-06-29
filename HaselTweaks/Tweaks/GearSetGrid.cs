using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

public partial class GearSetGrid(
    PluginConfig pluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ICommandManager CommandManager,
    AddonObserver AddonObserver,
    GearSetGridWindow Window)
    : IConfigurableTweak
{
    public string InternalName => nameof(GearSetGrid);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized; // TODO: fix icon overlays

    public void OnInitialize() { }

    public void OnEnable()
    {
        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;

        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            Window.Open();

        if (Config.RegisterCommand)
            CommandManager.AddHandler("/gsg", new(OnGsgCommand) { HelpMessage = TextService.Translate("GearSetGrid.CommandHandlerHelpMessage") });
    }

    public void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;

        Window.Close();

        if (Status == TweakStatus.Enabled && Config.RegisterCommand)
            CommandManager.RemoveHandler("/gsg");
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

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

    private void OnGsgCommand(string command, string arguments)
    {
        if (Window.IsOpen)
            Window.Close();
        else
            Window.Open();
    }
}
