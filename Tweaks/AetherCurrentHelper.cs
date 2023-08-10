using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using HaselTweaks.Windows;
using Lumina.Excel.GeneratedSheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class AetherCurrentHelper : Tweak
{
    public class Configuration
    {
        [BoolConfig]
        public bool AlwaysShowDistance = false;

        [BoolConfig]
        public bool CenterDistance = true;
    }

    private AetherCurrentHelperWindow? _window;

    public override void Disable()
        => CloseWindow();

    public void OpenWindow(AetherCurrentCompFlgSet compFlgSet)
    {
        if (_window != null)
        {
            _window.SetCompFlgSet(compFlgSet);
            return;
        }

        Plugin.WindowSystem.AddWindow(_window = new(this, compFlgSet));
        _window.IsOpen = true;
    }

    public void CloseWindow()
    {
        if (_window == null)
            return;

        Plugin.WindowSystem.RemoveWindow(_window);
        _window = null;
    }

    [VTableHook<AgentAetherCurrent>((int)AgentInterfaceVfs.ReceiveEvent)]
    private AtkValue* AgentAetherCurrent_ReceiveEvent(AgentAetherCurrent* agent, AtkValue* eventData, AtkValue* atkValue, uint valueCount, nint eventKind)
    {
        if (Service.KeyState[VirtualKey.SHIFT])
            goto OriginalCode;

        if (atkValue == null || atkValue->Type != ValueType.Int || atkValue->Int != 0)
            goto OriginalCode;

        var atkValue2 = (AtkValue*)((nint)atkValue + 0x10);
        if (atkValue2->Type != ValueType.Int)
            goto OriginalCode;

        var rawIndex = (uint)(atkValue2->Int + 6 * agent->TabIndex);
        var index = rawIndex + 1;
        if (index < 19)
            index = rawIndex;

        var compFlgSet = GetRow<AetherCurrentCompFlgSet>(index + 1);
        if (compFlgSet == null)
            goto OriginalCode;

        OpenWindow(compFlgSet);

        // handled, just like in the original code
        eventData->Type = ValueType.Bool;
        eventData->Byte = 0;
        return eventData;

OriginalCode:
        return AgentAetherCurrent_ReceiveEventHook.OriginalDisposeSafe(agent, eventData, atkValue, valueCount, eventKind);
    }
}
