using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class SimpleAethernetList : Tweak
{
    [VTableHook<AddonTeleportTown>((int)AtkUnitBaseVfs.ReceiveEvent)]
    private void AddonTeleportTown_ReceiveEvent(AddonTeleportTown* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if (eventType == AtkEventType.ListItemRollOver)
        {
            var agent = GetAgent<AgentTelepotTown>();
            var index = *(uint*)(a5 + 0x10);
            if (agent->Data != null && index >= 0)
            {
                var item = addon->List->GetItem(index);
                if (item != null && item->UIntValues.Size() >= 4)
                {
                    agent->Data->SelectedAetheryte = (byte)item->UIntValues.Get(3); 
                    agent->Data->Flags |= 2;
                    return;
                }
            }
        }

        AddonTeleportTown_ReceiveEventHook.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, a5);
    }
}
