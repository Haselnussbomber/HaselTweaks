using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class HideMSQComplete : Tweak
{
    public override string Name => "Hide MSQ Complete";
    public override string Description => "Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed.";

    private AgentScenarioTree* agent;

    public override void Setup()
    {
        agent = GetAgent<AgentScenarioTree>(AgentId.ScenarioTree);
    }

    public override void Disable()
    {
        UpdateVisibility(true);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        UpdateVisibility(agent->Data != null && agent->Data->NextId != 0);
    }

    private void UpdateVisibility(bool visible)
    {
        var addon = GetAddon<AtkUnitBase>((AgentInterface*)agent);
        if (addon == null)
            return;

        SetVisibility(addon, 11, visible); // AtkTextNode
        SetVisibility(addon, 12, visible); // AtkNineGridNode
        SetVisibility(addon, 13, visible); // AtkComponentButton
    }
}
