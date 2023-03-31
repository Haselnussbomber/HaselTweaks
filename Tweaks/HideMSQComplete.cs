using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class HideMSQComplete : Tweak
{
    public override string Name => "Hide MSQ Complete";
    public override string Description => "Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed.";

    private AgentScenarioTree* agentScenarioTree;

    public override void Setup()
    {
        GetAgent(AgentId.ScenarioTree, out agentScenarioTree);
    }

    public override void Disable()
    {
        UpdateVisibility(true);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        UpdateVisibility(agentScenarioTree->Data != null && agentScenarioTree->Data->NextId != 0);
    }

    private void UpdateVisibility(bool visible)
    {
        if (!GetAddon((AgentInterface*)agentScenarioTree, out var addon))
            return;

        SetVisibility(addon, 11, visible); // AtkTextNode
        SetVisibility(addon, 12, visible); // AtkNineGridNode
        SetVisibility(addon, 13, visible); // AtkComponentButton
    }
}
