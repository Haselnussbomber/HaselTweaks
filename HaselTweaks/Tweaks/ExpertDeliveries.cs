using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public sealed unsafe class ExpertDeliveries(ILogger<ExpertDeliveries> Logger, AddonObserver AddonObserver) : ITweak
{
    public string InternalName => nameof(ExpertDeliveries);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        AddonObserver.AddonOpen += OnAddonOpen;
    }

    public void OnDisable()
    {
        AddonObserver.AddonOpen -= OnAddonOpen;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    public void OnAddonOpen(string addonName)
    {
        if (addonName != "GrandCompanySupplyList")
            return;

        if (!TryGetAddon<AtkUnitBase>(addonName, out var addon))
            return;

        // prevent item selection for controller users to reset to the first entry
        if (*(short*)&AgentGrandCompanySupply.Instance()->SelectedTab == 2)
            return;

        Logger.LogDebug("Changing tab...");

        var atkEvent = new AtkEvent();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, &atkEvent);
    }
}
