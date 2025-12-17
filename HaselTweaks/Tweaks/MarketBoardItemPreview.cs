using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MarketBoardItemPreview : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;

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
        var item = new ItemHandle(AgentItemSearch.Instance()->ListingPageItemIds[itemIndex]);
        _logger.LogTrace("Previewing Index {atkEventData} with ItemId {itemId} @ {addr:X}", itemIndex, item.ItemId, args.Addon + itemIndex * 4 + 0xBBC);

        if (!item.CanTryOn)
        {
            _logger.LogInformation("Skipping preview of {name}, because it can't be tried on", item.Name);
            return;
        }

        AgentTryon.TryOn(args.Addon.Id, item, 0, 0, 0);
    }
}
