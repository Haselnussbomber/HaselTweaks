using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Windows;
using Lumina.Excel.GeneratedSheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

public sealed class AetherCurrentHelperConfiguration
{
    [BoolConfig]
    public bool AlwaysShowDistance = false;

    [BoolConfig]
    public bool CenterDistance = true;
}

public sealed unsafe class AetherCurrentHelper(
    IGameInteropProvider GameInteropProvider,
    IKeyState KeyState,
    PluginConfig PluginConfig,
    TranslationManager TranslationManager,
    AetherCurrentHelperWindow Window)
    : Tweak<AetherCurrentHelperConfiguration>(PluginConfig, TranslationManager)
{
    private Hook<AgentAetherCurrent.Delegates.ReceiveEvent>? ReceiveEventHook;

    public override void OnInitialize()
    {
        ReceiveEventHook = GameInteropProvider.HookFromAddress<AgentAetherCurrent.Delegates.ReceiveEvent>(
            AgentAetherCurrent.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public override void OnEnable()
    {
        ReceiveEventHook?.Enable();
    }

    public override void OnDisable()
    {
        ReceiveEventHook?.Disable();
        Window.Close();
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

    public bool OpenWindow(AgentAetherCurrent* agent, AtkValue* atkValue)
    {
        if (KeyState[VirtualKey.SHIFT])
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

        Window.CompFlgSet = compFlgSet;
        Window.Open();

        return true;
    }
}
