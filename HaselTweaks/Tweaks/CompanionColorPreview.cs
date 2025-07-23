using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CompanionColorPreview : Tweak
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public override void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        if (_addonObserver.IsAddonVisible("Buddy"))
            _windowManager.CreateOrOpen<CompanionColorPreviewWindow>();
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _windowManager.Close<CompanionColorPreviewWindow>();
    }

    private void OnAddonOpen(string addonName)
    {
        if (addonName != "Buddy")
            return;

        _windowManager.CreateOrOpen<CompanionColorPreviewWindow>();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName != "Buddy")
            return;

        _windowManager.Close<CompanionColorPreviewWindow>();
    }
}
