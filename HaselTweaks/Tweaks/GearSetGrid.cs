using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services.Commands;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GearSetGrid : ConfigurableTweak<GearSetGridConfiguration>
{
    private readonly AddonObserver _addonObserver;
    private readonly CommandService _commandService;
    private readonly GearSetGridWindow _window;

    private CommandHandler _gsgCommand;

    public override void OnEnable()
    {
        _disposables = DisposableBag.Create(
            _gsgCommand = _commandService.AddCommand("gsg", cmd => cmd
                .WithHelpTextKey("GearSetGrid.CommandHandlerHelpMessage")
                .WithDisplayOrder(2)
                .WithHandler(OnGsgCommand)
                .SetEnabled(_config.RegisterCommand)),

            _addonObserver.OnShow(OnShow, "GearSetList"),
            _addonObserver.OnHide(OnHide, "GearSetList"));

        if (_config.AutoOpenWithGearSetList && IsAddonOpen("GearSetList"u8))
            _window.Open();
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _window.Close();
    }

    private void OnGsgCommand(CommandContext ctx)
    {
        _window.Toggle();
    }

    private void OnShow(AtkUnitBase* addon)
    {
        if (_config.AutoOpenWithGearSetList)
            _window.Open();
    }

    private void OnHide(AtkUnitBase* addon)
    {
        if (_config.AutoOpenWithGearSetList)
            _window.Close();
    }
}
