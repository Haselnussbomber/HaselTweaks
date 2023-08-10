using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Utils;

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

        using var atkEvent = new DisposableStruct<AtkEvent>();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, atkEvent, 0);
    }
}
