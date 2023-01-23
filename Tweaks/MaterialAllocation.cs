using Dalamud.Game;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class MaterialAllocation : Tweak
{
    public override string Name => "Material Allocation";
    public override string Description => "Always opens the Island Sanctuarys \"Material Allocation\" window on the \"Current & Next Season\" tab.";

    private bool switched;
    public override void OnFrameworkUpdate(Framework framework)
    {
        var agent = GetAgent<AgentMJICraftSchedule>();
        if (!agent->AgentInterface.IsAgentActive())
        {
            if (switched) switched = false;
            return;
        }

        if (switched || agent->Data == null || agent->Data->IsLoading)
            return;

        var addon = GetAddon<AddonMJICraftMaterialConfirmation>("MJICraftMaterialConfirmation");
        if (!IsAddonReady(addon->AtkUnitBase.ID) || addon->RadioButton3 == null)
            return;

        if (agent->TabIndex != 2)
            addon->SwitchTab(2);

        switched = true;
    }
}
