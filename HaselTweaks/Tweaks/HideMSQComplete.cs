using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class HideMSQComplete : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);
        Update();
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);

        if (Status is TweakStatus.Enabled)
            UpdateVisibility(true, true);
    }

    private void ScenarioTree_PostRefresh(AddonEvent type, AddonArgs args)
    {
        Update();
    }

    private static void Update()
    {
        var agent = AgentScenarioTree.Instance();
        if (agent->Data == null)
            return;

        var data = agent->Data;
        var hasMainQuest = data->MainScenarioQuestIds[data->MSQPathIndex] != 0;
        var hasJobQuest = data->JobQuestIds[data->JobQuestIndex] != 0;
        UpdateVisibility(hasMainQuest, hasJobQuest);
    }

    private static void UpdateVisibility(bool hasMainQuest, bool hasJobQuest)
    {
        if (!TryGetAddon<AtkUnitBase>(AgentId.ScenarioTree, out var addon))
            return;

        addon->GetNodeById(11)->ToggleVisibility(hasMainQuest); // AtkTextNode (title)
        addon->GetNodeById(12)->ToggleVisibility(hasMainQuest); // AtkNineGridNode (title background)
        addon->GetNodeById(13)->ToggleVisibility(hasMainQuest); // AtkComponentButton (quest button)

        var lowerBox = addon->GetNodeById(6);
        var isInputHintShown = !hasMainQuest && lowerBox != null && lowerBox->Timeline != null && lowerBox->Timeline->ActiveLabelId == 102;
        addon->GetNodeById(9)->ToggleVisibility(!isInputHintShown); // AtkResNode (hide input hint)

        if (!hasMainQuest && !hasJobQuest)
            addon->Flags1A0 |= 0b1000_0000; // disable focusability
        else
            addon->Flags1A0 &= unchecked((byte)~0b1000_0000); // enable focusability
    }
}
