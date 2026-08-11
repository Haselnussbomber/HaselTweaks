using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class CabinetQuickStore : ConfigurableTweak<CabinetQuickStoreConfiguration>
{
    private readonly AddonObserver _addonObserver;
    private readonly WindowManager _windowManager;

    public override void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;

        if (_addonObserver.IsAddonVisible("Cabinet"))
            _windowManager.CreateOrOpen<CabinetQuickStoreWindow>();
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;

        _windowManager.Close<CabinetQuickStoreWindow>();
    }

    private void OnAddonOpen(string addonName)
    {
        if (addonName != "Cabinet")
            return;

        _windowManager.CreateOrOpen<CabinetQuickStoreWindow>();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName != "Cabinet")
            return;

        _windowManager.Close<CabinetQuickStoreWindow>();
    }
}
