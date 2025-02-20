using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Windows;
using Lumina.Excel.Sheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class AetherCurrentHelper(
    PluginConfig PluginConfig,
    IGameInteropProvider GameInteropProvider,
    IKeyState KeyState,
    ExcelService ExcelService,
    AetherCurrentHelperWindow Window,
    ConfigGui ConfigGui)
    : IConfigurableTweak
{
    public string InternalName => nameof(AetherCurrentHelper);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized; // needs updated agent

    private Hook<AgentAetherCurrent.Delegates.ReceiveEvent>? ReceiveEventHook;

    public void OnInitialize()
    {
        ReceiveEventHook = GameInteropProvider.HookFromAddress<AgentAetherCurrent.Delegates.ReceiveEvent>(
            AgentAetherCurrent.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public void OnEnable()
    {
        ReceiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        ReceiveEventHook?.Disable();
        Window.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        ReceiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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

        var firstAtkValue = atkValue[0];
        if (firstAtkValue.Type != ValueType.Int || firstAtkValue.Int != 0)
            return false;

        var secondAtkValue = atkValue[1];
        if (secondAtkValue.Type != ValueType.Int)
            return false;

        var rawIndex = (uint)(secondAtkValue.Int + 6 * agent->TabIndex);
        var index = rawIndex + 1;
        if (index < 19)
            index = rawIndex;

        if (!ExcelService.TryGetRow<AetherCurrentCompFlgSet>(index + 1, out var compFlgSet))
            return false;

        Window.CompFlgSet = compFlgSet;
        Window.Open();

        return true;
    }
}
