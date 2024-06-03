using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe class ExpertDeliveries : Tweak
{
    public override void OnAddonOpen(string addonName)
    {
        if (addonName != "GrandCompanySupplyList")
            return;

        if (!TryGetAddon<AtkUnitBase>(addonName, out var addon))
            return;

        var atkEvent = new AtkEvent();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, &atkEvent, 0);
    }
}
