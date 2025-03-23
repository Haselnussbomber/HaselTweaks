using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SimpleAethernetList : ITweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    private Hook<AddonTeleportTown.Delegates.ReceiveEvent>? _receiveEventHook;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _receiveEventHook = _gameInteropProvider.HookFromAddress<AddonTeleportTown.Delegates.ReceiveEvent>(
            AddonTeleportTown.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public void OnEnable()
    {
        _receiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        _receiveEventHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        _receiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
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
