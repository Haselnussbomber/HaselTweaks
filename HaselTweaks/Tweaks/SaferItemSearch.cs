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

public unsafe class SaferItemSearch(IAddonLifecycle AddonLifecycle, MarketBoardService MarketBoardService) : ITweak
{
    public string InternalName => nameof(SaferItemSearch);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private bool IsSearching;

    public void OnInitialize() { }

    public void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        MarketBoardService.ListingsStart += OnListingsStart;
        MarketBoardService.ListingsEnd += OnListingsEnd;
    }

    public void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        MarketBoardService.ListingsStart -= OnListingsStart;
        MarketBoardService.ListingsEnd -= OnListingsEnd;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void ItemSearch_PostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonItemSearch*)args.Addon;
        if (addon == null)
            return;

        for (var i = 0; i < addon->ResultsList->GetItemCount(); i++)
        {
            addon->ResultsList->SetItemDisabledState(i, IsSearching);
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

        addon->ComparePrices->AtkComponentBase.SetEnabledState(!IsSearching);
    }

    private void OnListingsStart()
    {
        IsSearching = true;
        UpdateRetainerSellButton();
    }

    private void OnListingsEnd(IReadOnlyList<IMarketBoardItemListing> listings)
    {
        IsSearching = false;
        UpdateRetainerSellButton();
    }
}
