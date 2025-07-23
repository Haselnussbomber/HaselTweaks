using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AetherCurrentHelper : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ExcelService _excelService;
    private readonly ConfigGui _configGui;
    private readonly WindowManager _windowManager;
    private readonly IServiceProvider _serviceProvider;

    private AetherCurrentHelperWindow? _window;

    private Hook<AgentAetherCurrent.Delegates.ReceiveEvent>? _receiveEventHook;

    public override void OnEnable()
    {
        _receiveEventHook = _gameInteropProvider.HookFromAddress<AgentAetherCurrent.Delegates.ReceiveEvent>(
            AgentAetherCurrent.StaticVirtualTablePointer->ReceiveEvent,
            ReceiveEventDetour);
        _receiveEventHook.Enable();
    }

    public override void OnDisable()
    {
        _receiveEventHook?.Dispose();
        _receiveEventHook = null;
        _window?.Dispose();
        _window = null;
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
        if (UIInputData.Instance()->IsKeyDown(SeVirtualKey.SHIFT))
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

        _window ??= _windowManager.CreateOrOpen<AetherCurrentHelperWindow>();

        if (_window.CompFlgSet?.RowId == compFlgSet.RowId)
            _window.Toggle();
        else
            _window.Open();

        _window.CompFlgSet = compFlgSet;

        return true;
    }
}
