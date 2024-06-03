using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using HaselCommon.Utils;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class SaferItemSearch : Tweak
{
    private bool _isSearching;

    private AddressHook<InfoProxyItemSearch.Delegates.ProcessRequestResult>? ProcessRequestResultHook;
    private VFuncHook<InfoProxyItemSearch.Delegates.EndRequest>? EndRequestHook;
    private VFuncHook<InfoProxyItemSearch.Delegates.AddPage>? AddPageHook;

    public override void SetupHooks()
    {
        ProcessRequestResultHook = new(InfoProxyItemSearch.MemberFunctionPointers.ProcessRequestResult, ProcessRequestResultDetour);
        EndRequestHook = new(InfoProxyItemSearch.StaticVirtualTablePointer, 6, EndRequestDetour);
        AddPageHook = new(InfoProxyItemSearch.StaticVirtualTablePointer, 12, AddPageDetour);
    }

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

    public nint ProcessRequestResultDetour(InfoProxyItemSearch* ipis, nint a2, nint a3, nint a4, int a5, byte a6, int a7)
    {
        _isSearching = true;

        UpdateRetainerSellButton();

        return ProcessRequestResultHook!.Original(ipis, a2, a3, a4, a5, a6, a7);
    }

    public void EndRequestDetour(InfoProxyItemSearch* ipis)
    {
        _isSearching = false;

        UpdateRetainerSellButton();

        EndRequestHook!.Original(ipis);
    }

    public void AddPageDetour(InfoProxyItemSearch* ipis, nint data)
    {
        _isSearching = true;

        UpdateRetainerSellButton();

        AddPageHook!.Original(ipis, data);
    }
}
