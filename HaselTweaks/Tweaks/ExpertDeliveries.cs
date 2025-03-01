using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ExpertDeliveries : ITweak
{
    private readonly ILogger<ExpertDeliveries> _logger;
    private readonly AddonObserver _addonObserver;

    public string InternalName => nameof(ExpertDeliveries);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
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
