using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class SimpleAethernetList : Tweak
{
    private VFuncHook<AddonTeleportTown.Delegates.ReceiveEvent>? ReceiveEventHook;

    public override void SetupHooks()
    {
        ReceiveEventHook = new(AddonTeleportTown.StaticVirtualTablePointer, (int)AtkUnitBaseVfs.ReceiveEvent, ReceiveEventDetour);
    }

    private void ReceiveEventDetour(AddonTeleportTown* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint atkEventData)
    {
        if (eventType == AtkEventType.ListItemRollOver)
        {
            var agent = GetAgent<AgentTelepotTown>();
            var index = *(uint*)(atkEventData + 0x10);
            if (agent->Data != null && index >= 0)
            {
                var item = addon->List->GetItem(index);
                if (item != null && item->UIntValues.LongCount >= 4)
                {
                    agent->Data->SelectedAetheryte = (byte)item->UIntValues[3];
                    agent->Data->Flags |= 2;
                    return;
                }
            }
        }

        ReceiveEventHook!.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, atkEventData);
    }
}
