using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class HideMSQComplete : Tweak
{
    public override void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);
        Update();
    }

    public override void Disable()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);
        UpdateVisibility(true);
    }

    private void ScenarioTree_PostRefresh(AddonEvent type, AddonArgs args)
    {
        Update();
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
