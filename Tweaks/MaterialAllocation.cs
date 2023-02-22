using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class MaterialAllocation : Tweak
{
    public override string Name => "Material Allocation";
    public override string Description => "Always opens the Island Sanctuarys \"Material Allocation\" window on the \"Current & Next Season\" tab.";

    private AgentMJICraftSchedule* agent;

    public override void Setup()
    {
        agent = GetAgent<AgentMJICraftSchedule>(AgentId.MJICraftSchedule);
    }

    // vf48 = OnOpen?
    [AutoHook, Signature("BA ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC 40 57 48 83 EC 20 48 8B F9 85 D2 7E 51", DetourName = nameof(AddonMJICraftMaterialConfirmation_vf48Detour))]
    private Hook<AddonMJICraftMaterialConfirmation_vf48Delegate> AddonMJICraftMaterialConfirmation_vf48Hook { get; init; } = null!;
    private delegate nint AddonMJICraftMaterialConfirmation_vf48Delegate(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, nint atkValues);
    public nint AddonMJICraftMaterialConfirmation_vf48Detour(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, nint atkValues)
    {
        agent->TabIndex = 2;

        for (var i = 0; i < 3; i++)
        {
            var button = addon->RadioButtonsSpan[i];
            if (button.Value != null)
            {
                button.Value->SetSelected(i == 2);
            }
        }

        return AddonMJICraftMaterialConfirmation_vf48Hook.Original(addon, numAtkValues, atkValues);
    }
}
