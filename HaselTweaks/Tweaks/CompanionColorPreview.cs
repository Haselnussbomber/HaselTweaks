using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CompanionColorPreview : Tweak
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public override ValueTask OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonObserver.OnShow(OnShow, "Buddy"),
            _addonObserver.OnHide(OnHide, "Buddy"));

        if (IsAddonOpen("Buddy"))
            _windowManager.CreateOrOpen<CompanionColorPreviewWindow>();

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _windowManager.Close<CompanionColorPreviewWindow>();

        return ValueTask.CompletedTask;
    }

    private void OnShow(AtkUnitBase* addon)
    {
        _windowManager.CreateOrOpen<CompanionColorPreviewWindow>();
    }

    private void OnHide(AtkUnitBase* addon)
    {
        _windowManager.Close<CompanionColorPreviewWindow>();
    }
}
