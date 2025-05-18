using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CompanionColorPreview : ITweak
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        if (_addonObserver.IsAddonVisible("Buddy"))
            _windowManager.CreateOrOpen<CompanionColorPreviewWindow>();
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _windowManager.Close<CompanionColorPreviewWindow>();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
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
