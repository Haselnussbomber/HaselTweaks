using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GlamourDresserAlert : ConfigurableTweak<GlamourDresserAlertConfiguration>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameInventory _gameInventory;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly CabinetService _cabinetService;
    private readonly IFramework _framework;

    private GlamourDresserAlertWindow? _window;
    private bool _isPendingUpdate;
    private uint[]? _lastItemIds = null;

    public Dictionary<uint, HashSet<ItemHandle>> CabinetStorableItems { get; } = [];
    public Dictionary<uint, HashSet<ItemHandle>> OutfitConvertibleItems { get; } = [];

    public override void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;
        _gameInventory.InventoryChangedRaw += OnInventoryUpdate;
        _framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;
        _gameInventory.InventoryChangedRaw -= OnInventoryUpdate;
        _framework.Update -= OnFrameworkUpdate;

        _isPendingUpdate = false;
        _window?.Dispose();
        _window = null;
    }

    private void OnAddonOpen(string addonName)
    {
        _isPendingUpdate |= addonName == "MiragePrismPrismBox";
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "MiragePrismPrismBox")
        {
            _lastItemIds = null;
            _isPendingUpdate = false;
            _window?.Close();
        }
    }

    private void OnInventoryUpdate(IReadOnlyCollection<InventoryEventArgs> events)
    {
        _isPendingUpdate |= _addonObserver.IsAddonVisible("MiragePrismPrismBox");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var mirageManager = MirageManager.Instance();
        if (!mirageManager->PrismBoxLoaded)
            return;

        var itemIds = mirageManager->PrismBoxItemIds;

        if (!_isPendingUpdate || (_lastItemIds != null && mirageManager->PrismBoxItemIds.SequenceEqual(_lastItemIds)))
            return;

        _isPendingUpdate = true;
        _lastItemIds = itemIds.ToArray();

        CabinetStorableItems.Clear();
        OutfitConvertibleItems.Clear();

        _logger.LogInformation("Updating...");

        for (var i = 0; i < itemIds.Length; i++)
        {
            ItemHandle item = itemIds[i];

            // skip empty slots
            if (item.IsEmpty)
                continue;

            // check if item exists and has UI category set
            if (!_itemService.TryGetItem(item, out var itemRow) || itemRow.ItemUICategory.RowId == 0 || !itemRow.ItemUICategory.IsValid)
                continue;

            var isSet = _excelService.TryGetRow<MirageStoreSetItem>(item, out var setRow);

            // skip if user rather wants to collect outfits
            if (_config.IgnoreOutfits && isSet)
                continue;

            // skip items that are dyed
            if (_config.IgnoreDyedGlamour && !isSet && (mirageManager->PrismBoxStain0Ids[i] != 0 || mirageManager->PrismBoxStain1Ids[i] != 0))
                continue;

            if (_config.IgnoreDuplicates && !isSet && _cabinetService.IsItemCollected(item))
                continue;

            if (_config.IgnoreDuplicates && isSet && setRow.Items.Where(item => item.RowId != 0 && item.IsValid).Any(item => _cabinetService.IsItemCollected(item)))
                continue;

            if (ProcessCabinetStorableItem(item, itemRow, isSet, setRow))
                continue;

            ProcessOutfitConvertibleItem(item, itemRow);
        }

        _window?.IsUpdatePending = false;

        if (CabinetStorableItems.Count == 0 && OutfitConvertibleItems.Count == 0)
            return;

        _window ??= _serviceProvider.CreateInstance<GlamourDresserAlertWindow>(this);
        _window.Open();
    }

    private bool ProcessCabinetStorableItem(ItemHandle item, Item itemRow, bool isSet, MirageStoreSetItem setRow)
    {
        if (isSet)
        {
            // skip outfits without items that can be stored in the armoire
            if (!setRow.Items.Any(item => _cabinetService.TryGetCabinetId(item, out _)))
                return false;
        }
        else
        {
            // skip items that can't be stored in the armoire
            if (!_cabinetService.TryGetCabinetId(item, out _))
                return false;
        }

        if (!CabinetStorableItems.TryGetValue(itemRow.ItemUICategory.RowId, out var categoryItems))
            CabinetStorableItems.TryAdd(itemRow.ItemUICategory.RowId, categoryItems = []);

        categoryItems.Add(item);
        return true;
    }

    private void ProcessOutfitConvertibleItem(ItemHandle item, Item itemRow)
    {
        if (!_excelService.TryGetRow<MirageStoreSetItemLookup>(item, out var setsRow))
            return;

        if (!setsRow.Item.Any(setItem => setItem.RowId != 0 && setItem.IsValid))
            return;

        if (!OutfitConvertibleItems.TryGetValue(itemRow.ItemUICategory.RowId, out var categoryItems))
            OutfitConvertibleItems.TryAdd(itemRow.ItemUICategory.RowId, categoryItems = []);

        categoryItems.Add(item);
    }
}
