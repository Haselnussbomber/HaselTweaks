using HaselCommon.Services.Commands;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class GearSetGrid : ConfigurableTweak<GearSetGridConfiguration>
{
    private readonly CommandService _commandService;
    private readonly AddonObserver _addonObserver;
    private readonly GearSetGridWindow _window;

    private CommandHandler _gsgCommand;

    public override void OnEnable()
    {
        _gsgCommand = _commandService.AddCommand("gsg", cmd => cmd
            .WithHelpTextKey("GearSetGrid.CommandHandlerHelpMessage")
            .WithDisplayOrder(2)
            .WithHandler(OnGsgCommand)
            .SetEnabled(_config.RegisterCommand));

        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        if (_config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"u8))
            _window.Open();
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _gsgCommand?.Dispose();

        _window.Close();
    }

    private void OnAddonOpen(string addonName)
    {
        if (_config.AutoOpenWithGearSetList && addonName == "GearSetList")
            _window.Open();
    }

    private void OnAddonClose(string addonName)
    {
        if (_config.AutoOpenWithGearSetList && addonName == "GearSetList")
            _window.Close();
    }

    private void OnGsgCommand(CommandContext ctx)
    {
        _window.Toggle();
    }
}
