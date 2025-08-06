using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MaterialAllocation : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly IAddonLifecycle _addonLifecycle;

    private MaterialAllocationConfiguration Config => _pluginConfig.Tweaks.MaterialAllocation;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PostReceiveEvent);
        _addonLifecycle.RegisterListener(AddonEvent.PreSetup, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PreSetup);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PostReceiveEvent);
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PreSetup);
    }

    private void AddonMJICraftMaterialConfirmation_PreSetup(AddonEvent type, AddonArgs args)
    {
        if (Config.LastSelectedTab > 2)
            Config.LastSelectedTab = 2;

        AgentMJICraftSchedule.Instance()->CurReviewMaterialsTab = Config.LastSelectedTab;

        var addon = (AddonMJICraftMaterialConfirmation*)args.Addon.Address;
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
