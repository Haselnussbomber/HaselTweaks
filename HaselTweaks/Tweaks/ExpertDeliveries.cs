using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ExpertDeliveries : Tweak
{
    private readonly AddonObserver _addonObserver;

    public override void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
    }

    public void OnAddonOpen(string addonName)
    {
        if (addonName != "GrandCompanySupplyList")
            return;

        if (!TryGetAddon<AtkUnitBase>(addonName, out var addon))
            return;

        // prevent item selection for controller users to reset to the first entry
        if (AgentGrandCompanySupply.Instance()->SelectedTab == 2)
            return;

        _logger.LogDebug("Changing tab...");

        var atkEvent = new AtkEvent();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, &atkEvent);
    }
}
