using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class MaterialAllocation : Tweak
{
    public override string Name => "Material Allocation";
    public override string Description => "Always opens the Island Sanctuarys \"Material Allocation\" window on the \"Current & Next Season\" tab.";

    private AgentMJICraftSchedule* agent;
    private bool switched;

    public override void Setup()
    {
        agent = GetAgent<AgentMJICraftSchedule>(AgentId.MJICraftSchedule);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = GetAddon<AddonMJICraftMaterialConfirmation>("MJICraftMaterialConfirmation");
        if (addon == null)
        {
            if (switched)
                switched = false;

            return;
        }

        if (switched || agent->Data == null || agent->Data->IsLoading || addon->RadioButton3 == null)
            return;

        if (agent->TabIndex != 2)
        {
            Log("window opened, switching tab");
            addon->SwitchTab(2);
        }

        switched = true;
    }
}
