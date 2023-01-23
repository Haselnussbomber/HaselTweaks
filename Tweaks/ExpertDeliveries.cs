using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class ExpertDeliveries : Tweak
{
    public override string Name => "Expert Deliveries";
    public override string Description => "Always opens the \"Grand Company Delivery Missions\" window on the \"Expert Delivery\" tab.";

    private bool switched;

    public override unsafe void OnFrameworkUpdate(Framework framework)
    {
        var agent = GetAgent<AgentGrandCompanySupply>(AgentId.GrandCompanySupply);
        if (!agent->AgentInterface.IsAgentActive())
        {
            if (switched) switched = false;
            return;
        }

        if (switched)
            return;

        var addon = agent->GetAddon();
        if (addon == null)
            return;

        Log("window opened, switching tab");

        var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, atkEvent, 0);
        IMemorySpace.Free(atkEvent);

        switched = true;
    }
}
