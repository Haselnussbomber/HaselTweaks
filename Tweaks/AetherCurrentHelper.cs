using System;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Windows;
using Lumina.Excel.GeneratedSheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public unsafe class AetherCurrentHelper : Tweak
{
    public override string Name => "Aether Current Helper";
    public override string Description => "Clicking on a zone in the Aether Currents window opens a helper window that shows where to find the aether currents or which quests unlocks them. Clicking on an aether current in the list flags the position of the aether current or the quest giver on the map.";

    public class Configuration
    {
        [ConfigField(Label = "Show distance instead of checkmark when unlocked")]
        public bool AlwaysShowDistance = false;

        [ConfigField(Label = "Center distance column", Description = "Disable this if you have problems with the window endlessly expanding to the right")]
        public bool CenterDistance = true;
    }

    private readonly AetherCurrentHelperWindow Window = new();

    // Client::UI::Agent::AgentAetherCurrent_ReceiveEvent
    [AutoHook, Signature("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B D9 49 8B F8", DetourName = nameof(ReceiveEventDetour))]
    private Hook<ReceiveEventDelegate> ReceiveEventHook { get; init; } = null!;
    private unsafe delegate IntPtr ReceiveEventDelegate(AgentAetherCurrent* agent, IntPtr a2, AtkValue* a3);

    public override void Enable()
    {
        Plugin.WindowSystem.AddWindow(Window);
    }

    public override void Disable()
    {
        Plugin.WindowSystem.RemoveWindow(Window);
    }

    private unsafe IntPtr ReceiveEventDetour(AgentAetherCurrent* agent, IntPtr a2, AtkValue* atkValue)
    {
        if (Service.KeyState[VirtualKey.SHIFT])
            goto OriginalCode;

        if (atkValue == null)
            goto OriginalCode;

        if (atkValue->Type != ValueType.Int || atkValue->Int != 0)
            goto OriginalCode;

        var atkValue2 = (AtkValue*)((IntPtr)atkValue + 0x10);
        if (atkValue2->Type != ValueType.Int)
            goto OriginalCode;

        var rawIndex = (uint)(atkValue2->Int + 6 * agent->TabIndex);
        var index = rawIndex + 1;
        if (index < 19)
            index = rawIndex;

        var compFlgSet = Service.Data.GetExcelSheet<AetherCurrentCompFlgSet>()?.GetRow(index + 1);
        if (compFlgSet == null)
            goto OriginalCode;

        Window.SetCompFlgSet(compFlgSet);

        if (!Window.IsOpen)
            Window.Toggle();

        // handled, just like in the original code
        *(uint*)a2 = 2;
        *(byte*)(a2 + 8) = 0;
        return a2;

        OriginalCode:
        return ReceiveEventHook.Original(agent, a2, atkValue);
    }
}
