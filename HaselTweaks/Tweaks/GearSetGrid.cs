using HaselCommon.Commands;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class GearSetGrid : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly CommandService _commandService;
    private readonly AddonObserver _addonObserver;
    private readonly GearSetGridWindow _window;

    private CommandHandler? _gsgCommand;

    public override void OnEnable()
    {
        _gsgCommand = _commandService.Register(OnGsgCommand);
        _gsgCommand.SetEnabled(Config.RegisterCommand);

        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        if (Config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"))
            _window.Open();
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _gsgCommand?.Dispose();
        _gsgCommand = null;

        _window.Close();
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
