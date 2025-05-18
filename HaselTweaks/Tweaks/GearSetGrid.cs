using HaselCommon.Commands;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class GearSetGrid : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly CommandService _commandService;
    private readonly AddonObserver _addonObserver;
    private readonly GearSetGridWindow _window;

    private CommandHandler? _gsgCommand;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _gsgCommand = _commandService.Register(OnGsgCommand);
    }

    public void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        _gsgCommand?.SetEnabled(Config.RegisterCommand);

        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            _window.Open();
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _gsgCommand?.SetEnabled(false);

        _window.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _gsgCommand?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void OnAddonOpen(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            _window.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (Config.AutoOpenWithGearSetList && addonName == "GearSetList")
            _window.Close();
    }

    [CommandHandler("/gsg", "GearSetGrid.CommandHandlerHelpMessage", DisplayOrder: 2)]
    private void OnGsgCommand(string command, string arguments)
    {
        if (_window.IsOpen)
            _window.Close();
        else
            _window.Open();
    }
}
