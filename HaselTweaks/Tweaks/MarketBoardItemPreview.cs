using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MarketBoardItemPreview : Tweak
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ItemService _itemService;

    public override ValueTask OnEnable()
    {
        _disposables = _addonLifecycle.OnPostReceiveEvent(OnPostReceiveEvent, "ItemSearch");

        return ValueTask.CompletedTask;
    }

    public override ValueTask OnDisable()
    {
        DisposeAndNull(ref _disposables);

        return ValueTask.CompletedTask;
    }

    private void OnPostReceiveEvent(AddonReceiveEventArgs args)
    {
        if (args.EventType != AtkEventType.ListItemRollOver)
            return;

        var eventData = args.GetEventData<AtkEventData.AtkListItemData>();
        var itemIndex = eventData->SelectedIndex;
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
