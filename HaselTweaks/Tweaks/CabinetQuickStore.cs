using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CabinetQuickStore : ConfigurableTweak<CabinetQuickStoreConfiguration>
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public override ValueTask OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonObserver.OnShow(OnShow, "Cabinet"),
            _addonObserver.OnHide(OnHide, "Cabinet"));

        if (IsAddonOpen("Cabinet"))
            _windowManager.CreateOrOpen<CabinetQuickStoreWindow>();

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _windowManager.Close<CabinetQuickStoreWindow>();

        return ValueTask.CompletedTask;
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
