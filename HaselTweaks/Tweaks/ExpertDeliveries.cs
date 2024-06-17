using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public sealed unsafe class ExpertDeliveries(AddonObserver AddonObserver) : ITweak
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

    public void Dispose()
    {
        OnDisable();
    }

    public void OnAddonOpen(string addonName)
    {
        if (addonName != "GrandCompanySupplyList")
            return;

        if (!TryGetAddon<AtkUnitBase>(addonName, out var addon))
            return;

        var atkEvent = new AtkEvent();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, &atkEvent, 0);
    }
}
