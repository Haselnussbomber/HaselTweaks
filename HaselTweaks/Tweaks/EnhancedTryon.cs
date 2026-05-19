using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using AgentId = Dalamud.Game.Agent.AgentId;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedTryon : ConfigurableTweak<EnhancedTryonConfiguration>
{
    private readonly IAgentLifecycle _agentLifecycle;
    private bool _doUpdate;

    public override void OnEnable()
    {
        _agentLifecycle.RegisterListener(AgentEvent.PreUpdate, AgentId.Tryon, OnPreUpdate);
        _agentLifecycle.RegisterListener(AgentEvent.PostUpdate, AgentId.Tryon, OnPostUpdate);
    }

    public override void OnDisable()
    {
        _agentLifecycle.UnregisterListener(AgentEvent.PreUpdate, AgentId.Tryon, OnPreUpdate);
        _agentLifecycle.UnregisterListener(AgentEvent.PostUpdate, AgentId.Tryon, OnPostUpdate);
    }

    private void OnPreUpdate(AgentEvent type, AgentArgs args)
    {
        var agent = args.GetAgentPointer<AgentTryon>();
        _doUpdate = agent->CharaView.DoUpdate;
    }

    private void OnPostUpdate(AgentEvent type, AgentArgs args)
    {
        if (!_doUpdate || !_config.KeepFacewearOn)
            return;

        var agent = args.GetAgentPointer<AgentTryon>();
        if (!agent->CharaView.HideOtherEquipment)
            return;

        var character = agent->CharaView.GetCharacter();
        if (character == null || character->DrawData.GlassesIds[0] != 0)
            return;

        // if we're trying on glasses, bail out
        foreach (ref var slot in agent->TryOnItems)
        {
            if (slot.EquipSlotCategory == 13)
                return;
        }

        // otherwise, find glasses and put them on
        for (byte i = 0; i < agent->GearItems.Length; i++)
        {
            ref var slot = ref agent->GearItems[i];

            if (slot.EquipSlotCategory != 13)
                continue;

            _logger.LogDebug("Keeping glasses on...");
            character->DrawData.SetGlasses(0, (ushort)slot.Id);
            return;
        }

        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer != null && localPlayer->DrawData.GlassesIds[0] != 0)
        {
            _logger.LogDebug("Keeping glasses on... (fallback)");
            character->DrawData.SetGlasses(0, localPlayer->DrawData.GlassesIds[0]);
        }
    }
}
