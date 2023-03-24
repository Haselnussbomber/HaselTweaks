using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class ExpertDeliveries : Tweak
{
    public override string Name => "Expert Deliveries";
    public override string Description => "Always opens the \"Grand Company Delivery Missions\" window on the \"Expert Delivery\" tab.";

    public override void OnAddonOpen(string addonName, AtkUnitBase* unitBase)
    {
        if (addonName != "GrandCompanySupplyList")
            return;

        var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
        ((AddonGrandCompanySupplyList*)unitBase)->ReceiveEvent(AtkEventType.ButtonClick, 4, atkEvent, 0);
        IMemorySpace.Free(atkEvent);
    }
}
