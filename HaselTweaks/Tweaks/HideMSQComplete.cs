using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[Tweak]
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
        var agentScenarioTree = GetAgent<AgentScenarioTree>();
        UpdateVisibility(agentScenarioTree->Data != null && agentScenarioTree->Data->CurrentScenarioQuest != 0);
    }

    private static void UpdateVisibility(bool visible)
    {
        if (!TryGetAddon<AtkUnitBase>(AgentId.ScenarioTree, out var addon))
            return;

        GetNode<AtkResNode>(addon, 11)->ToggleVisibility(visible); // AtkTextNode
        GetNode<AtkResNode>(addon, 12)->ToggleVisibility(visible); // AtkNineGridNode
        GetNode<AtkResNode>(addon, 13)->ToggleVisibility(visible); // AtkComponentButton
    }
}
