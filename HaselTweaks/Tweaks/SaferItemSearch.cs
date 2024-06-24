using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public unsafe class SaferItemSearch(IGameInteropProvider GameInteropProvider, IAddonLifecycle AddonLifecycle) : ITweak
{
    public string InternalName => nameof(SaferItemSearch);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private bool _isSearching;

    private Hook<InfoProxyItemSearch.Delegates.ProcessRequestResult>? ProcessRequestResultHook;
    private Hook<InfoProxyItemSearch.Delegates.EndRequest>? EndRequestHook;
    private Hook<InfoProxyItemSearch.Delegates.AddPage>? AddPageHook;

    public void OnInitialize()
    {
        ProcessRequestResultHook = GameInteropProvider.HookFromAddress<InfoProxyItemSearch.Delegates.ProcessRequestResult>(
            InfoProxyItemSearch.MemberFunctionPointers.ProcessRequestResult,
            ProcessRequestResultDetour);

        EndRequestHook = GameInteropProvider.HookFromAddress<InfoProxyItemSearch.Delegates.EndRequest>(
            InfoProxyItemSearch.StaticVirtualTablePointer->EndRequest,
            EndRequestDetour);

        AddPageHook = GameInteropProvider.HookFromAddress<InfoProxyItemSearch.Delegates.AddPage>(
            InfoProxyItemSearch.StaticVirtualTablePointer->AddPage,
            AddPageDetour);
    }

    public void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        ProcessRequestResultHook?.Enable();
        EndRequestHook?.Enable();
        AddPageHook?.Enable();
    }

    public void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemSearch", ItemSearch_PostRequestedUpdate);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", RetainerSell_PostSetup);

        ProcessRequestResultHook?.Disable();
        EndRequestHook?.Disable();
        AddPageHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status == TweakStatus.Disposed)
            return;

        OnDisable();
        ProcessRequestResultHook?.Dispose();
        EndRequestHook?.Dispose();
        AddPageHook?.Dispose();

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

    private nint ProcessRequestResultDetour(InfoProxyItemSearch* ipis, nint a2, nint a3, nint a4, int a5, byte a6, int a7)
    {
        _isSearching = true;

        UpdateRetainerSellButton();

        return ProcessRequestResultHook!.Original(ipis, a2, a3, a4, a5, a6, a7);
    }

    private void EndRequestDetour(InfoProxyItemSearch* ipis)
    {
        _isSearching = false;

        UpdateRetainerSellButton();

        EndRequestHook!.Original(ipis);
    }

    private void AddPageDetour(InfoProxyItemSearch* ipis, nint data)
    {
        _isSearching = true;

        UpdateRetainerSellButton();

        AddPageHook!.Original(ipis, data);
    }
}
