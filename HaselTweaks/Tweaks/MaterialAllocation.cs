using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MaterialAllocation : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly IAddonLifecycle _addonLifecycle;

    public string InternalName => nameof(MaterialAllocation);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private MaterialAllocationConfiguration Config => _pluginConfig.Tweaks.MaterialAllocation;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PostReceiveEvent);
        _addonLifecycle.RegisterListener(AddonEvent.PreSetup, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PreSetup);
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PostReceiveEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PreSetup);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void AddonMJICraftMaterialConfirmation_PreSetup(AddonEvent type, AddonArgs args)
    {
        if (Config.LastSelectedTab > 2)
            Config.LastSelectedTab = 2;

        AgentMJICraftSchedule.Instance()->CurReviewMaterialsTab = Config.LastSelectedTab;

        var addon = (AddonMJICraftMaterialConfirmation*)args.Addon;
        for (var i = 0; i < 3; i++)
        {
            var button = addon->RadioButtons.GetPointer(i);
            if (button->Value != null)
            {
                button->Value->IsSelected = i == Config.LastSelectedTab;
            }
        }
    }

    private void AddonMJICraftMaterialConfirmation_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (type != AddonEvent.PostReceiveEvent || args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.EventParam is not > 0 or not < 4)
            return;

        Config.LastSelectedTab = (byte)(receiveEventArgs.EventParam - 1);
        _pluginConfig.Save();
    }
}
