using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Network.Structures;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SaferItemSearch : ITweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly MarketBoardService _marketBoardService;

    private bool _isSearching;

    public string InternalName => nameof(SaferItemSearch);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        _marketBoardService.ListingsStart += OnListingsStart;
        _marketBoardService.ListingsEnd += OnListingsEnd;
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        _marketBoardService.ListingsStart -= OnListingsStart;
        _marketBoardService.ListingsEnd -= OnListingsEnd;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void ItemSearch_PostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonItemSearch*)args.Addon;
        if (addon == null)
            return;

        for (var i = 0; i < addon->ResultsList->GetItemCount(); i++)
        {
            addon->ResultsList->SetItemDisabledState(i, _isSearching);
        }
    }

    private void RetainerSell_PostSetup(AddonEvent type, AddonArgs args)
    {
        UpdateRetainerSellButton((AddonRetainerSell*)args.Addon);
    }

    private void UpdateRetainerSellButton(AddonRetainerSell* addon = null)
    {
        if (addon == null)
            addon = GetAddon<AddonRetainerSell>("RetainerSell");

        if (addon == null)
            return;

        addon->ComparePrices->AtkComponentBase.SetEnabledState(!_isSearching);
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
