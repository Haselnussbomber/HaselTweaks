using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class MaterialAllocation : Tweak
{
    public override string Name => "Material Allocation";
    public override string Description => "Always opens the Island Sanctuarys \"Material Allocation\" window on the \"Current & Next Season\" tab.";

    private bool switched;
    public override void OnFrameworkUpdate(Framework framework)
    {
        var agent = GetAgent(AgentId.MJICraftSchedule);
        if (!agent->IsAgentActive())
        {
            if (switched) switched = false;
            return;
        }

        var agentMJICraftSchedule = (AgentMJICraftSchedule*)agent;

        if (switched || agentMJICraftSchedule->Data == null || agentMJICraftSchedule->Data->IsLoading)
            return;

        var addon = GetAddon<AddonMJICraftMaterialConfirmation>("MJICraftMaterialConfirmation");
        if (!IsAddonReady(addon->AtkUnitBase.ID) || addon->RadioButton3 == null)
            return;

        if (agentMJICraftSchedule->TabIndex != 2)
            addon->SwitchTab(2);

        switched = true;
    }
}
