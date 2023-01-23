using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class HideMSQComplete : Tweak
{
    public override string Name => "Hide MSQ Complete";
    public override string Description => "Hides the Main Scenario Guide when the MSQ is completed. Job quests are still being displayed.";

    private AtkComponentButton* buttonNode;
    private AtkNineGridNode* msqCompleteNineGridNode;
    private AtkTextNode* msqCompleteTextNode;

    private Lazy<string> msqCompleteText => new(() => Service.StringUtils.GetSheetText<Addon>(5672, "Text") ?? "Main Scenario Quests Complete");

    public override void Disable()
    {
        UpdateVisibility(true);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = GetAddon<AtkUnitBase>(AgentId.ScenarioTree);
        if (addon == null)
            return;

        msqCompleteTextNode = GetNode<AtkTextNode>(addon, 11);
        msqCompleteNineGridNode = GetNode<AtkNineGridNode>(addon, 12);
        buttonNode = GetNode<AtkComponentButton>(addon, 13);

        if (msqCompleteTextNode == null)
            return;

        var text = msqCompleteTextNode->NodeText.ToString();
        var isMSQIncomplete = text != msqCompleteText.Value;

        UpdateVisibility(isMSQIncomplete);
    }

    private void UpdateVisibility(bool visible)
    {
        SetVisibility((AtkResNode*)msqCompleteNineGridNode, visible);
        SetVisibility((AtkResNode*)msqCompleteTextNode, visible);
        SetVisibility((AtkResNode*)buttonNode, visible);
    }
}
