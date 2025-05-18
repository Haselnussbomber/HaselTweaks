using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MarketBoardItemPreview : ITweak
{
    private readonly ILogger<MarketBoardItemPreview> _logger;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ItemService _itemService;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "ItemSearch", ItemSearch_PostReceiveEvent);
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "ItemSearch", ItemSearch_PostReceiveEvent);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void ItemSearch_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs addonReceiveEventArgs || addonReceiveEventArgs.AtkEventType != (byte)AtkEventType.ListItemRollOver)
            return;

        var eventData = (AtkEventData*)addonReceiveEventArgs.Data;
        var itemIndex = eventData->ListItemData.SelectedIndex;
        var itemId = AgentItemSearch.Instance()->ListingPageItemIds[itemIndex];
        _logger.LogTrace("Previewing Index {atkEventData} with ItemId {itemId} @ {addr:X}", itemIndex, itemId, args.Addon + itemIndex * 4 + 0xBBC);

        if (!_itemService.CanTryOn(itemId))
        {
            _logger.LogInformation("Skipping preview of {name}, because it can't be tried on", _textService.GetItemName(itemId));
            return;
        }

        AgentTryon.TryOn(((AtkUnitBase*)args.Addon)->Id, itemId, 0, 0, 0);
    }
}
