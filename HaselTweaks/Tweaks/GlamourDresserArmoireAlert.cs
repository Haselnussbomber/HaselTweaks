using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GlamourDresserArmoireAlert : ITweak
{
    private readonly ILogger<GlamourDresserArmoireAlert> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameInventory _gameInventory;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;

    private GlamourDresserArmoireAlertWindow? _window;
    private uint[]? _lastItemIds = null;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public Dictionary<uint, Dictionary<uint, (Item Item, bool IsHq)>> Categories { get; } = [];

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;
        _gameInventory.InventoryChangedRaw += OnInventoryUpdate;
    }

    public void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;
        _gameInventory.InventoryChangedRaw -= OnInventoryUpdate;

        _window?.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void OnAddonOpen(string addonName)
    {
        if (addonName == "MiragePrismPrismBox")
            Update();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "MiragePrismPrismBox")
        {
            _lastItemIds = null;
            _window?.Close();
        }
    }

    private void OnInventoryUpdate(IReadOnlyCollection<InventoryEventArgs> events)
    {
        if (_addonObserver.IsAddonVisible("MiragePrismPrismBox"))
            Update();
    }

    public void Update()
    {
        var itemIds = MirageManager.Instance()->PrismBoxItemIds;

        if (_lastItemIds != null && itemIds.SequenceEqual(_lastItemIds))
            return;

        _lastItemIds = itemIds.ToArray();

        Categories.Clear();

        _logger.LogInformation("Updating...");

        for (var i = 0u; i < itemIds.Length; i++)
        {
            var (itemId, itemKind) = ItemUtil.GetBaseId(itemIds[(int)i]);

            if (itemId == 0)
                continue;

            if (!_excelService.TryGetRow<Item>(itemId, out var item))
                continue;

            if (!_excelService.TryFindRow<Cabinet>(row => row.Item.RowId == itemId, out var cabinet))
                continue;

            if (!Categories.TryGetValue(item.ItemUICategory.RowId, out var categoryItems))
                Categories.TryAdd(item.ItemUICategory.RowId, categoryItems = []);

            if (!categoryItems.ContainsKey(i))
                categoryItems.Add(i, (item, itemKind.HasFlag(ItemKind.Hq)));
        }

        if (_window != null)
            _window.IsUpdatePending = false;

        if (Categories.Count == 0)
            return;

        _window ??= ActivatorUtilities.CreateInstance<GlamourDresserArmoireAlertWindow>(_serviceProvider, this);
        _window.Open();
    }
}
