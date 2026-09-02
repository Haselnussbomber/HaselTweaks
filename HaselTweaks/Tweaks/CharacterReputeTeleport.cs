using Dalamud.Game.Agent.AgentArgTypes;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharacterReputeTeleport : Tweak
{
    private readonly IAgentLifecycle _agentLifecycle;
    private readonly IKeyState _keyState;
    private readonly ExcelService _excelService;
    private readonly TeleportService _teleportService;

    public override void OnEnable()
    {
        _disposables = _agentLifecycle.OnPreReceiveEvent(OnStatusReceiveEvent, AgentId.Status);
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
    }

    private void OnStatusReceiveEvent(AgentReceiveEventArgs args)
    {
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
