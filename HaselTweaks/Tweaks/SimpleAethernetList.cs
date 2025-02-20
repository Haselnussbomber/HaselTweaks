using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe class SimpleAethernetList(IGameInteropProvider GameInteropProvider) : ITweak
{
    public string InternalName => nameof(SimpleAethernetList);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private Hook<AddonTeleportTown.Delegates.ReceiveEvent>? ReceiveEventHook;

    public void OnInitialize()
    {
        ReceiveEventHook = GameInteropProvider.HookFromAddress<AddonTeleportTown.Delegates.ReceiveEvent>(
            AddonTeleportTown.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public void OnEnable()
    {
        ReceiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        ReceiveEventHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        ReceiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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

        ReceiveEventHook!.Original(addon, eventType, eventParam, atkEvent, atkEventData);
    }
}
