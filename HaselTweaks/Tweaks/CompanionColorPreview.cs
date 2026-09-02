using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CompanionColorPreview : Tweak
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public override void OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonObserver.OnShow(OnShow, "Buddy"),
            _addonObserver.OnHide(OnHide, "Buddy"));

        if (IsAddonOpen("Buddy"))
            _windowManager.CreateOrOpen<CompanionColorPreviewWindow>();
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
        _windowManager.Close<CompanionColorPreviewWindow>();
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
