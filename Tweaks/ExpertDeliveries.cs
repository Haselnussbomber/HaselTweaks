using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Expert Deliveries",
    Description: "Always opens the \"Grand Company Delivery Missions\" window on the \"Expert Delivery\" tab."
)]
public unsafe class ExpertDeliveries : Tweak
{
    public override void OnAddonOpen(string addonName, AtkUnitBase* unitBase)
    {
        if (addonName != "GrandCompanySupplyList")
            return;

        using var atkEvent = new DisposableStruct<AtkEvent>();
        unitBase->ReceiveEvent(AtkEventType.ButtonClick, 4, atkEvent, 0);
    }
}
