using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Windows;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GlamourDresserArmoireAlert : ITweak
{
    private readonly ILogger<GlamourDresserArmoireAlert> _logger;
    private readonly IGameInventory _gameInventory;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly WindowManager _windowManager;
    private readonly TextureService _textureService;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly ImGuiContextMenuService _imGuiContextMenuService;

    private bool _isPrismBoxOpen;
    private uint[]? _lastItemIds = null;

    public string InternalName => nameof(GlamourDresserArmoireAlert);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public Dictionary<uint, Dictionary<uint, (Item Item, bool IsHq)>> Categories { get; } = [];
    public bool UpdatePending { get; set; } // used to disable ImGui.Selectables after clicking to restore an item

    public void OnInitialize() { }

    public void OnEnable()
    {
        _isPrismBoxOpen = _addonObserver.IsAddonVisible("MiragePrismPrismBox");

        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;
        _gameInventory.InventoryChangedRaw += OnInventoryUpdate;
    }

    public void OnDisable()
    {
        _isPrismBoxOpen = false;

        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;
        _gameInventory.InventoryChangedRaw -= OnInventoryUpdate;

        _windowManager.Close<GlamourDresserArmoireAlertWindow>();
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
        if (addonName != "MiragePrismPrismBox")
            return;

        _isPrismBoxOpen = true;
        Update();
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName != "MiragePrismPrismBox")
            return;

        _isPrismBoxOpen = false;
        _lastItemIds = null;

        _windowManager.Close<GlamourDresserArmoireAlertWindow>();
    }

    private void OnInventoryUpdate(IReadOnlyCollection<InventoryEventArgs> events)
    {
        if (!_isPrismBoxOpen)
            return;

        Update();
    }

    public void Update()
    {
        const int NumPrismBoxSlots = 800;
        var mirageManager = MirageManager.Instance();

        var itemIds = mirageManager->PrismBoxItemIds;
        if (_lastItemIds != null && itemIds.SequenceEqual(_lastItemIds))
            return;

        _lastItemIds = itemIds.ToArray();

        Categories.Clear();

        _logger.LogInformation("Updating...");

        for (var i = 0u; i < NumPrismBoxSlots; i++)
        {
            var itemId = mirageManager->PrismBoxItemIds[(int)i];
            if (itemId == 0)
                continue;

            var isHq = itemId is > 1000000 and < 1500000;
            itemId %= 1000000;

            if (!_excelService.TryGetRow<Item>(itemId, out var item))
                continue;

            if (!_excelService.TryFindRow<Cabinet>(row => row.Item.RowId == itemId, out var cabinet))
                continue;

            if (!Categories.TryGetValue(item.ItemUICategory.RowId, out var categoryItems))
                Categories.TryAdd(item.ItemUICategory.RowId, categoryItems = []);

            if (!categoryItems.ContainsKey(i))
                categoryItems.Add(i, (item, isHq));
        }

        UpdatePending = false;

        if (Categories.Count == 0)
            return;

        _logger.LogTrace("Open!!!");

        _windowManager.CreateOrOpen(() => new GlamourDresserArmoireAlertWindow(_windowManager, _textService, _languageProvider, _textureService, _excelService, _imGuiContextMenuService, this));
    }
}
