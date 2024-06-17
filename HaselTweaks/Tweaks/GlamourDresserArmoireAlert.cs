using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Windows;
using Lumina.Excel.GeneratedSheets;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public sealed unsafe class GlamourDresserArmoireAlert(
    ILogger<GlamourDresserArmoireAlert> Logger,
    IGameInventory GameInventory,
    AddonObserver AddonObserver,
    GlamourDresserArmoireAlertWindow Window)
    : ITweak
{
    private bool _isPrismBoxOpen;
    private uint[]? _lastItemIds = null;

    public string InternalName => nameof(GlamourDresserArmoireAlert);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public Dictionary<uint, Dictionary<uint, (ExtendedItem Item, bool IsHq)>> Categories { get; } = [];
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

    public void Dispose()
    {
        OnDisable();
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

            var item = GetRow<ExtendedItem>(itemId);
            if (item == null)
                continue;

            var cabinet = FindRow<Cabinet>(row => row?.Item.Row == itemId);
            if (cabinet == null)
                continue;

            if (!Categories.TryGetValue(item.ItemUICategory.Row, out var categoryItems))
                Categories.TryAdd(item.ItemUICategory.Row, categoryItems = []);

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
