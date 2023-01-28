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

    private AgentGrandCompanySupply* agent;
    private bool switched;

    public override void Setup()
    {
        agent = GetAgent<AgentGrandCompanySupply>(AgentId.GrandCompanySupply);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = agent->GetAddon();
        if (addon == null)
        {
            if (switched)
                switched = false;

            return;
        }

        if (switched)
            return;

        Log("window opened, switching tab");

        var atkEvent = (AtkEvent*)IMemorySpace.GetUISpace()->Malloc<AtkEvent>();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, atkEvent, 0);
        IMemorySpace.Free(atkEvent);

        switched = true;
    }
}
