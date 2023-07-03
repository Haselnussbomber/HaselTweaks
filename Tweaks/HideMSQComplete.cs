using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Hide MSQ Complete",
    Description: "Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed."
)]
public unsafe class HideMSQComplete : Tweak
{
    public override void Disable()
    {
        UpdateVisibility(true);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        if (GetAgent<AgentScenarioTree>(AgentId.ScenarioTree, out var agentScenarioTree))
            UpdateVisibility(agentScenarioTree->Data != null && agentScenarioTree->Data->NextId != 0);
    }

    private static void UpdateVisibility(bool visible)
    {
        if (!GetAddon(AgentId.ScenarioTree, out var addon))
            return;

        SetVisibility(addon, 11, visible); // AtkTextNode
        SetVisibility(addon, 12, visible); // AtkNineGridNode
        SetVisibility(addon, 13, visible); // AtkComponentButton
    }
}
