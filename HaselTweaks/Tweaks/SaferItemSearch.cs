using Dalamud.Game.Network.Structures;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SaferItemSearch : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly MarketBoardService _marketBoardService;

    private bool _isSearching;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        _marketBoardService.ListingsStart += OnListingsStart;
        _marketBoardService.ListingsEnd += OnListingsEnd;
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        _marketBoardService.ListingsStart -= OnListingsStart;
        _marketBoardService.ListingsEnd -= OnListingsEnd;
    }

    private void ItemSearch_PostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonItemSearch*)args.Addon.Address;
        if (addon == null)
            return;

        for (var i = 0; i < addon->ResultsList->GetItemCount(); i++)
        {
            addon->ResultsList->SetItemDisabledState(i, _isSearching);
        }
    }

    private void RetainerSell_PostSetup(AddonEvent type, AddonArgs args)
    {
        UpdateRetainerSellButton((AddonRetainerSell*)args.Addon.Address);
    }

    private void UpdateRetainerSellButton(AddonRetainerSell* addon = null)
    {
        if (addon == null)
            addon = GetAddon<AddonRetainerSell>("RetainerSell");

        if (addon == null)
            return;

        addon->ComparePrices->SetEnabledState(!_isSearching);
    }

    private void OnListingsStart()
    {
        _isSearching = true;
        UpdateRetainerSellButton();
    }

    private void OnListingsEnd(IReadOnlyList<IMarketBoardItemListing> listings)
    {
        _isSearching = false;
        UpdateRetainerSellButton();
    }
}
