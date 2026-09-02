using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MaterialAllocation : ConfigurableTweak<MaterialAllocationConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _disposables = DisposableBag.Create(
            _addonLifecycle.OnPostReceiveEvent(OnPostReceiveEvent, "MJICraftMaterialConfirmation"),
            _addonLifecycle.OnPreSetup(OnPreSetup, "MJICraftMaterialConfirmation"));
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
    }

    private void OnPreSetup(AddonArgs args)
    {
        if (_config.LastSelectedTab > 2)
            _config.LastSelectedTab = 2;

        AgentMJICraftSchedule.Instance()->CurReviewMaterialsTab = _config.LastSelectedTab;

        var addon = args.GetAddon<AddonMJICraftMaterialConfirmation>();
        for (var i = 0; i < addon->RadioButtons.Length; i++)
        {
            var button = addon->RadioButtons[i];
            if (button.Value != null)
                button.Value->IsSelected = i == _config.LastSelectedTab;
        }
    }

    private void OnPostReceiveEvent(AddonReceiveEventArgs args)
    {
        if (args.EventParam is not > 0 or not < 4)
            return;

        _config.LastSelectedTab = (byte)(args.EventParam - 1);
        _pluginConfig.Save();
    }
}
