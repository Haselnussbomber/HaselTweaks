using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Windows;
using AgentId = Dalamud.Game.Agent.AgentId;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AetherCurrentHelper : ConfigurableTweak<AetherCurrentHelperConfiguration>
{
    private readonly ExcelService _excelService;
    private readonly WindowManager _windowManager;
    private readonly IAgentLifecycle _agentLifecycle;

    private AetherCurrentHelperWindow? _window;

    public override void OnEnable()
    {
        _agentLifecycle.RegisterListener(AgentEvent.PreReceiveEvent, AgentId.AetherCurrent, OnPreReceiveEvent);
    }

    public override void OnDisable()
    {
        _agentLifecycle.UnregisterListener(AgentEvent.PreReceiveEvent, AgentId.AetherCurrent, OnPreReceiveEvent);
        _window?.Dispose();
        _window = null;
    }

    private void OnPreReceiveEvent(AgentEvent type, AgentArgs agentArgs)
    {
        if (agentArgs is not AgentReceiveEventArgs args)
            return;

        var agent = args.GetAgentPointer<AgentAetherCurrent>();
        var values = args.GetAtkValues();

        if (UIInputData.Instance()->IsKeyDown(SeVirtualKey.SHIFT))
            return;

        if (values.Length < 2)
            return;

        if (!values[0].TryGetInt(out var eventType) || eventType != 0)
            return;

        if (!values[1].TryGetInt(out var buttonIndex))
            return;

        var rawIndex = (uint)(buttonIndex + 6 * agent->TabIndex);
        var index = rawIndex + 1;
        if (index < 19)
            index = rawIndex;

        if (!_excelService.TryGetRow<AetherCurrentCompFlgSet>(index + 1, out var compFlgSet))
            return;

        _window ??= _windowManager.CreateOrOpen<AetherCurrentHelperWindow>();

        if (_window.CompFlgSet?.RowId == compFlgSet.RowId)
            _window.Toggle();
        else
            _window.Open();

        _window.CompFlgSet = compFlgSet;

        values[0].SetInt(1337);
    }
}
