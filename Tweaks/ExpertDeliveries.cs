using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

public unsafe class ExpertDeliveries : Tweak
{
    public override string Name => "Expert Deliveries";
    public override string Description => "Always opens the \"Grand Company Delivery Missions\" window on the \"Expert Delivery\" tab.";

    private delegate void* ReceiveEventDelegate(IntPtr addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, IntPtr resNode);

    private bool switched;

    public override void OnFrameworkUpdate(Framework framework)
    {
        var unitBase = AtkUtils.GetUnitBase("GrandCompanySupplyList");
        if (unitBase == null || !unitBase->IsVisible)
        {
            if (switched) switched = false;
            return;
        }

        var someLoadedStateMaybe = MemoryHelper.Read<byte>((IntPtr)unitBase + 0x188);
        if (switched || someLoadedStateMaybe != 0x14) return;

        Log($"window opened, switching tab");

        var receiveEvent = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>((IntPtr)unitBase->AtkEventListener.vfunc[2]);
        receiveEvent((IntPtr)unitBase, AtkEventType.ButtonClick, 4, unitBase->RootNode->AtkEventManager.Event, (IntPtr)unitBase->RootNode);
        switched = true;
    }
}
