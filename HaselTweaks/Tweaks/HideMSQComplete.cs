using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public unsafe class HideMSQComplete(IAddonLifecycle AddonLifecycle) : ITweak
{
    public string InternalName => nameof(HideMSQComplete);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);
        Update();
    }

    public void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "ScenarioTree", ScenarioTree_PostRefresh);

        if (Status is TweakStatus.Enabled)
            UpdateVisibility(true);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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

        GetNode<AtkResNode>(addon, 11)->ToggleVisibility(visible); // AtkTextNode
        GetNode<AtkResNode>(addon, 12)->ToggleVisibility(visible); // AtkNineGridNode
        GetNode<AtkResNode>(addon, 13)->ToggleVisibility(visible); // AtkComponentButton
    }
}
