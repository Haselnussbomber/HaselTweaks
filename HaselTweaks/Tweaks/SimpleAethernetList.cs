using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SimpleAethernetList : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    private Hook<AddonTeleportTown.Delegates.ReceiveEvent>? _receiveEventHook;

    public override void OnEnable()
    {
        _receiveEventHook = _gameInteropProvider.HookFromAddress<AddonTeleportTown.Delegates.ReceiveEvent>(
            AddonTeleportTown.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
        _receiveEventHook.Enable();
    }

    public override void OnDisable()
    {
        _receiveEventHook?.Dispose();
        _receiveEventHook = null;
    }

    private void ReceiveEventDetour(AddonTeleportTown* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData)
    {
        if (eventType == AtkEventType.ListItemRollOver)
        {
            var agent = AgentTelepotTown.Instance();
            var index = atkEventData->ListItemData.SelectedIndex;
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

        _receiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }
}
