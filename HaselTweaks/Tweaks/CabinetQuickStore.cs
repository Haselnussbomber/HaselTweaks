using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CabinetQuickStore : ConfigurableTweak<CabinetQuickStoreConfiguration>
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public override void OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonObserver.OnShow(OnShow, "Cabinet"),
            _addonObserver.OnHide(OnHide, "Cabinet"));

        if (IsAddonOpen("Cabinet"))
            _windowManager.CreateOrOpen<CabinetQuickStoreWindow>();
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _windowManager.Close<CabinetQuickStoreWindow>();
    }

    private void OnShow(AtkUnitBase* addon)
    {
        _windowManager.CreateOrOpen<CabinetQuickStoreWindow>();
    }

    private void OnHide(AtkUnitBase* addon)
    {
        _windowManager.Close<CabinetQuickStoreWindow>();
    }
}
