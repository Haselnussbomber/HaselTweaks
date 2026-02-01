using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MarketBoardItemPreview : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ItemService _itemService;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "ItemSearch", ItemSearch_PostReceiveEvent);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "ItemSearch", ItemSearch_PostReceiveEvent);
    }

    private void ItemSearch_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonReceiveEventArgs addonReceiveEventArgs || addonReceiveEventArgs.AtkEventType != (byte)AtkEventType.ListItemRollOver)
            return;

        var eventData = (AtkEventData*)addonReceiveEventArgs.AtkEventData;
        var itemIndex = eventData->ListItemData.SelectedIndex;
        var itemId = AgentItemSearch.Instance()->ListingPageItemIds[itemIndex];

        if (!_itemService.CanTryOn(itemId))
        {
            _logger.LogInformation("Skipping preview of {name}, because it can't be tried on", _itemService.GetItemName(itemId, false));
            return;
        }

        _logger.LogTrace("Previewing Index {atkEventData} with ItemId {itemId}", itemIndex, itemId);

        AgentTryon.TryOn(args.Addon.Id, itemId, 0, 0, 0);
    }
}
