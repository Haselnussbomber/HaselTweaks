using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Hide MSQ Complete",
    Description: "Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed."
)]
public unsafe partial class HideMSQComplete : Tweak
{
    public override void Enable()
    {
        Update();
    }

    public override void Disable()
    {
        UpdateVisibility(true);
    }

    [VTableHook<AddonScenarioTree>((int)AtkUnitBaseVfs.OnRefresh)]
    public bool AddonScenarioTree_OnRefresh(AddonScenarioTree* addon, uint numValues, AtkValue* values)
    {
        var ret = AddonScenarioTree_OnRefreshHook.OriginalDisposeSafe(addon, numValues, values);

        Update();

        return ret;
    }

    private static void Update()
    {
        if (TryGetAgent<AgentScenarioTree>(AgentId.ScenarioTree, out var agentScenarioTree))
            UpdateVisibility(agentScenarioTree->Data != null && agentScenarioTree->Data->NextId != 0);
    }

    private static void UpdateVisibility(bool visible)
    {
        if (!TryGetAddon(AgentId.ScenarioTree, out var addon))
            return;

        SetVisibility(addon, 11, visible); // AtkTextNode
        SetVisibility(addon, 12, visible); // AtkNineGridNode
        SetVisibility(addon, 13, visible); // AtkComponentButton
    }
}
