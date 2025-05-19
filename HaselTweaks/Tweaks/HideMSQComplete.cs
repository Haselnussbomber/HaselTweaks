using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class HideMSQComplete : ITweak
{
    private readonly IAddonLifecycle _addonLifecycle;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);
        Update();
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);

        if (Status is TweakStatus.Enabled)
            UpdateVisibility(true);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void ScenarioTree_PostRefresh(AddonEvent type, AddonArgs args)
    {
        Update();
    }

    private static void Update()
    {
        var agentScenarioTree = AgentScenarioTree.Instance();
        UpdateVisibility(agentScenarioTree->Data != null && agentScenarioTree->Data->CurrentScenarioQuest != 0);
    }

    private static void UpdateVisibility(bool visible)
    {
        if (!TryGetAddon<AtkUnitBase>(AgentId.ScenarioTree, out var addon))
            return;

        addon->GetNodeById(11)->ToggleVisibility(visible); // AtkTextNode
        addon->GetNodeById(12)->ToggleVisibility(visible); // AtkNineGridNode
        addon->GetNodeById(13)->ToggleVisibility(visible); // AtkComponentButton
    }
}
