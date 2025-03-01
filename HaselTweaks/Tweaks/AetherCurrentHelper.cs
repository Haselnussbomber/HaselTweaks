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

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AetherCurrentHelper : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IKeyState _keyState;
    private readonly ExcelService _excelService;
    private readonly AetherCurrentHelperWindow _window;
    private readonly ConfigGui _configGui;

    private Hook<AgentAetherCurrent.Delegates.ReceiveEvent>? _receiveEventHook;

    public string InternalName => nameof(AetherCurrentHelper);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _receiveEventHook = _gameInteropProvider.HookFromAddress<AgentAetherCurrent.Delegates.ReceiveEvent>(
            AgentAetherCurrent.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
    }

    public void OnEnable()
    {
        _receiveEventHook?.Enable();
    }

    public void OnDisable()
    {
        _receiveEventHook?.Disable();
        _window.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _receiveEventHook?.Dispose();

        Status = TweakStatus.Disposed;
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

        return _receiveEventHook!.Original(agent, returnValue, values, valueCount, eventKind);
    }

    public bool OpenWindow(AgentAetherCurrent* agent, AtkValue* atkValue)
    {
        if (_keyState[VirtualKey.SHIFT])
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

        if (!_excelService.TryGetRow<AetherCurrentCompFlgSet>(index + 1, out var compFlgSet))
            return false;

        _window.CompFlgSet = compFlgSet;
        _window.Open();

        return true;
    }
}
