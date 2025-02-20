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

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe class GlamourDresserArmoireAlert(
    ILogger<GlamourDresserArmoireAlert> Logger,
    IGameInventory GameInventory,
    AddonObserver AddonObserver,
    GlamourDresserArmoireAlertWindow Window,
    ExcelService ExcelService)
    : ITweak
{
    public string InternalName => nameof(GlamourDresserArmoireAlert);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private bool _isPrismBoxOpen;
    private uint[]? _lastItemIds = null;

    public Dictionary<uint, Dictionary<uint, (Item Item, bool IsHq)>> Categories { get; } = [];
    public bool UpdatePending { get; set; } // used to disable ImGui.Selectables after clicking to restore an item

    public void OnInitialize()
    {
        Window.Tweak = this;
    }

    public void OnEnable()
    {
        _isPrismBoxOpen = AddonObserver.IsAddonVisible("MiragePrismPrismBox");

        AddonObserver.AddonOpen += OnAddonOpen;
        AddonObserver.AddonClose += OnAddonClose;
        GameInventory.InventoryChangedRaw += OnInventoryUpdate;
    }

    public void OnDisable()
    {
        _isPrismBoxOpen = false;

        AddonObserver.AddonOpen -= OnAddonOpen;
        AddonObserver.AddonClose -= OnAddonClose;
        GameInventory.InventoryChangedRaw -= OnInventoryUpdate;

        Window.Close();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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

        Window.Close();
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

        Logger.LogInformation("Updating...");

        for (var i = 0u; i < NumPrismBoxSlots; i++)
        {
            var itemId = mirageManager->PrismBoxItemIds[(int)i];
            if (itemId == 0)
                continue;

            var isHq = itemId is > 1000000 and < 1500000;
            itemId %= 1000000;

            if (!ExcelService.TryGetRow<Item>(itemId, out var item))
                continue;

            if (!ExcelService.TryFindRow<Cabinet>(row => row.Item.RowId == itemId, out var cabinet))
                continue;

            if (!Categories.TryGetValue(item.ItemUICategory.RowId, out var categoryItems))
                Categories.TryAdd(item.ItemUICategory.RowId, categoryItems = []);

            if (!categoryItems.ContainsKey(i))
                categoryItems.Add(i, (item, isHq));
        }

        UpdatePending = false;

        if (Categories.Count == 0)
            return;

        Logger.LogTrace("Open!!!");
        Window.Open();
    }
}
