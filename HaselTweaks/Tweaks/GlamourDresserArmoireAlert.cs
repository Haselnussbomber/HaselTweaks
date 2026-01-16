using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GlamourDresserArmoireAlert : Tweak
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameInventory _gameInventory;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly IFramework _framework;

    private GlamourDresserArmoireAlertWindow? _window;
    private bool _isPendingUpdate;
    private HashSet<uint>? _cabinetItems = null;
    private uint[]? _lastItemIds = null;

    public Dictionary<uint, HashSet<ItemHandle>> Categories { get; } = [];

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

        _cabinetItems ??= [.. _excelService.GetSheet<Cabinet>().Select(row => row.Item.RowId)];

        Categories.Clear();

        _logger.LogInformation("Updating...");

        for (var i = 0u; i < itemIds.Length; i++)
        {
            ItemHandle item = itemIds[(int)i];

            var itemId = item.BaseItemId;
            if (itemId == 0)
                continue;

            if (!item.TryGetItem(out var itemRow) && itemRow.ItemUICategory.TryGetRow(out var itemUICategory))
                continue;

            if (!_cabinetItems.Contains(itemId) && !IsSetContainingCabinetItems(itemId))
                continue;

            if (!Categories.TryGetValue(itemRow.ItemUICategory.RowId, out var categoryItems))
                Categories.TryAdd(itemRow.ItemUICategory.RowId, categoryItems = []);

            categoryItems.Add(item);
        }

        _window?.IsUpdatePending = false;

        if (Categories.Count == 0)
            return;

        _window ??= _serviceProvider.CreateInstance<GlamourDresserArmoireAlertWindow>(this);
        _window.Open();
    }

    private bool IsSetContainingCabinetItems(uint itemId)
    {
        if (!_excelService.TryGetRow<MirageStoreSetItem>(itemId, out var set))
            return false;
        
        if (!set.TryGetSetItemBitArray(out var unlockArray, false))
            return false;
        
        if (!set.Items.Where(setItem => setItem.RowId != 0).Any(setItem => _cabinetItems!.Contains(setItem.RowId)))
            return false;

        return true;
    }
}
