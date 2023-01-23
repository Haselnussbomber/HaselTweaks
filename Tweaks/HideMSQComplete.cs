using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class HideMSQComplete : Tweak
{
    public override string Name => "Hide MSQ Complete";
    public override string Description => "Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed.";

    private AtkTextNode* msqCompleteTextNode;
    private AtkNineGridNode* msqCompleteNineGridNode;
    private AtkComponentButton* buttonNode;

    private bool IsMSQIncomplete
    {
        get
        {
            var agent = GetAgent<AgentScenarioTree>(AgentId.ScenarioTree);
            if (agent == null || agent->Data == null)
                return false;

            if (msqCompleteTextNode == null || msqCompleteNineGridNode == null || buttonNode == null)
            {
                var addon = GetAddon<AtkUnitBase>(AgentId.ScenarioTree);
                if (addon == null)
                    return false;

                if (msqCompleteTextNode == null)
                    msqCompleteTextNode = GetNode<AtkTextNode>(addon, 11);

                if (msqCompleteNineGridNode == null)
                    msqCompleteNineGridNode = GetNode<AtkNineGridNode>(addon, 12);

                if (buttonNode == null)
                    buttonNode = GetNode<AtkComponentButton>(addon, 13);
            }

            return agent->Data->NextId != 0;
        }
    }

    public override void Disable()
    {
        UpdateVisibility(true);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        UpdateVisibility(IsMSQIncomplete);
    }

    private void UpdateVisibility(bool visible)
    {
        SetVisibility((AtkResNode*)msqCompleteNineGridNode, visible);
        SetVisibility((AtkResNode*)msqCompleteTextNode, visible);
        SetVisibility((AtkResNode*)buttonNode, visible);
    }
}
