using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using DAgentId = Dalamud.Game.Agent.AgentId;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharacterReputeTeleport : Tweak
{
    private readonly IAgentLifecycle _agentLifecycle;
    private readonly ExcelService _excelService;
    private readonly TeleportService _teleportService;
    private readonly IKeyState _keyState;

    public override void OnEnable()
    {
        _agentLifecycle.RegisterListener(AgentEvent.PreReceiveEvent, DAgentId.Status, OnStatusReceiveEvent);
    }

    public override void OnDisable()
    {
        _agentLifecycle.UnregisterListener(AgentEvent.PreReceiveEvent, DAgentId.Status, OnStatusReceiveEvent);
    }

    private void OnStatusReceiveEvent(AgentEvent type, AgentArgs eventArgs)
    {
        if (eventArgs is not AgentReceiveEventArgs args)
            return;

        if (args.EventKind != 0)
            return;

        var values = args.GetAtkValues();
        if (values.Length == 0)
            return;

        if (!values[0].TryGetInt(out var eventId) || eventId != 19)
            return;

        if (_keyState[VirtualKey.SHIFT] || _keyState[VirtualKey.LSHIFT] || _keyState[VirtualKey.RSHIFT])
            return;

        if (!values[1].TryGetUInt(out var beastTribeId))
            return;

        if (!_excelService.TryGetRow<BeastTribe>(beastTribeId, out var beastTribeRow) || !beastTribeRow.Level.IsValid)
            return;

        if (!_teleportService.TryGetClosestAetheryte(beastTribeRow.Level.Value, out var aetheryteRow))
            return;

        Telepo.Instance()->Teleport(aetheryteRow.RowId, 0);
        args.PreventOriginal();
    }
}
