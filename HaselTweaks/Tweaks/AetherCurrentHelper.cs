using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Enums;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Windows;
using Lumina.Excel.GeneratedSheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public class AetherCurrentHelperConfiguration
{
    [BoolConfig]
    public bool AlwaysShowDistance = false;

    [BoolConfig]
    public bool CenterDistance = true;
}

[Tweak]
public unsafe partial class AetherCurrentHelper : Tweak<AetherCurrentHelperConfiguration>
{
    private VFuncHook<AgentAetherCurrent.Delegates.ReceiveEvent>? ReceiveEventHook;

    public override void SetupHooks()
    {
        ReceiveEventHook = new(AgentAetherCurrent.StaticVirtualTablePointer, (int)AgentInterfaceVfs.ReceiveEvent, ReceiveEventDetour);
    }

    public override void Disable()
    {
        if (Service.HasService<WindowManager>())
            Service.WindowManager.CloseWindow<AetherCurrentHelperWindow>();
    }

    private AtkValue* ReceiveEventDetour(AgentAetherCurrent* agent, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        if (OpenWindow(agent, values))
        {
            // handled, just like in the original code
            returnValue->Type = ValueType.Bool;
            returnValue->Byte = 0;
            return returnValue;
        }

        return ReceiveEventHook!.Original(agent, returnValue, values, valueCount, eventKind);
    }

    public static bool OpenWindow(AgentAetherCurrent* agent, AtkValue* atkValue)
    {
        if (Service.KeyState[VirtualKey.SHIFT])
            return false;

        if (atkValue == null)
            return false;

        ref var firstAtkValue = ref atkValue[0];
        if (firstAtkValue.Type != ValueType.Int || firstAtkValue.Int != 0)
            return false;

        ref var secondAtkValue = ref atkValue[1];
        if (secondAtkValue.Type != ValueType.Int)
            return false;

        var rawIndex = (uint)(secondAtkValue.Int + 6 * agent->TabIndex);
        var index = rawIndex + 1;
        if (index < 19)
            index = rawIndex;

        var compFlgSet = GetRow<AetherCurrentCompFlgSet>(index + 1);
        if (compFlgSet == null)
            return false;

        var window = Service.WindowManager.OpenWindow<AetherCurrentHelperWindow>();
        window.CompFlgSet = compFlgSet;

        return true;
    }
}
