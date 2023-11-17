using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using HaselTweaks.Structs.Addons;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class SaferItemSearch : Tweak
{
    private bool _isSearching;

    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);
    }

    private void ItemSearch_PostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var addon = (AddonItemSearch*)args.Addon;
        if (addon == null)
            return;

        for (var i = 0; i < addon->SearchResultsList->GetItemCount(); i++)
        {
            addon->SearchResultsList->SetItemDisabledState(i, _isSearching);
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

        addon->CheckMarketPriceButton->AtkComponentBase.SetEnabledState(!_isSearching);
    }

    [AddressHook<InfoProxyItemSearch>(nameof(InfoProxyItemSearch.Addresses.ProcessRequestResult))]
    public void InfoProxyItemSearch_ProcessRequestResult(InfoProxyItemSearch* ipis, uint itemId, nint a3, nint a4, int a5, byte listingCount, int code)
    {
        _isSearching = true;

        UpdateRetainerSellButton();

        InfoProxyItemSearch_ProcessRequestResultHook.OriginalDisposeSafe(ipis, itemId, a3, a4, a5, listingCount, code);
    }

    [VTableHook<InfoProxyItemSearch>(6)]
    public void InfoProxyItemSearch_EndRequest(InfoProxyItemSearch* ipis)
    {
        _isSearching = false;

        UpdateRetainerSellButton();

        InfoProxyItemSearch_EndRequestHook.OriginalDisposeSafe(ipis);
    }

    [VTableHook<InfoProxyItemSearch>(12)]
    public void InfoProxyItemSearch_AddPage(InfoProxyItemSearch* ipis, nint data)
    {
        _isSearching = true;

        UpdateRetainerSellButton();

        InfoProxyItemSearch_AddPageHook.OriginalDisposeSafe(ipis, data);
    }
}
