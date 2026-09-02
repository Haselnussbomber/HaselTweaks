using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ExpertDeliveries : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly AddonObserver _addonObserver;

    public override void OnEnable()
    {
        _disposables = _addonObserver.OnShow(OnShow, "GrandCompanySupplyList");
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
    }

    private void OnShow(AtkUnitBase* addon)
    {
        // prevent item selection for controller users to reset to the first entry
        if (AgentGrandCompanySupply.Instance()->SelectedTab == 2)
            return;

        _logger.LogDebug("Changing tab...");

        var atkEvent = new AtkEvent();
        addon->ReceiveEvent(AtkEventType.ButtonClick, 4, &atkEvent);
    }
}
