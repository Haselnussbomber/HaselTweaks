using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class HideMSQComplete : Tweak
{
    public override string Name => "Hide MSQ Complete";
    public override string Description => "Hides the Main Scenario Guide when you've completed it. Job quests are still being displayed.";

    private enum NodeId : uint
    {
        Text = 11,
        NineGrid = 12,
        Button = 13,
    }

    private AtkResNode* buttonNode;
    private AtkNineGridNode* msqCompleteNineGridNode;
    private AtkTextNode* msqCompleteTextNode;

    private string msqCompleteText = "";

    public override void Setup()
    {
        msqCompleteText = Service.Data.GetExcelSheet<Addon>()?.GetRow(5672)?.Text ?? "Main Scenario Quests Complete";
    }

    public override void Disable()
    {
        UpdateVisibility(true);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = Utils.GetUnitBase("ScenarioTree");
        if (addon == null) return;

        msqCompleteTextNode = (AtkTextNode*)Utils.GetNode(addon, (uint)NodeId.Text);
        if (msqCompleteTextNode == null) return;

        msqCompleteNineGridNode = (AtkNineGridNode*)Utils.GetNode(addon, (uint)NodeId.NineGrid);
        if (msqCompleteNineGridNode == null) return;

        buttonNode = Utils.GetNode(addon, (uint)NodeId.Button);
        if (buttonNode == null) return;

        var text = msqCompleteTextNode->NodeText.ToString();
        var isMSQIncomplete = text != msqCompleteText;

        UpdateVisibility(isMSQIncomplete);
    }

    private void UpdateVisibility(bool visible)
    {
        Utils.SetVisibility(&msqCompleteNineGridNode->AtkResNode, visible);
        Utils.SetVisibility(&msqCompleteTextNode->AtkResNode, visible);
        Utils.SetVisibility(buttonNode, visible);
    }
}
