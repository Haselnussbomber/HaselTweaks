using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselTweaks.Windows;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class GlamourDresserArmoireAlert : Tweak
{
    private bool _isPrismBoxOpen;
    private uint[]? _lastItemIds = null;

    public Dictionary<uint, Dictionary<uint, (ExtendedItem Item, bool IsHq)>> Categories { get; } = [];
    public bool UpdatePending { get; set; } // used to disable ImGui.Selectables after clicking to restore an item

    public override void Enable()
    {
        _isPrismBoxOpen = IsAddonOpen("MiragePrismPrismBox");
    }

    public override void Disable()
    {
        _isPrismBoxOpen = false;

        if (Service.HasService<WindowManager>())
            Service.WindowManager.CloseWindow<GlamourDresserArmoireAlertWindow>();
    }

    public override void OnAddonOpen(string addonName)
    {
        if (addonName != "MiragePrismPrismBox")
            return;

        _isPrismBoxOpen = true;
        Update();
    }

    public override void OnAddonClose(string addonName)
    {
        if (addonName != "MiragePrismPrismBox")
            return;

        _isPrismBoxOpen = false;

        if (Service.HasService<WindowManager>())
            Service.WindowManager.CloseWindow<GlamourDresserArmoireAlertWindow>();
    }

    public override void OnInventoryUpdate()
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

        Log("Updating...");

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

        if (!Service.WindowManager.IsWindowOpen<GlamourDresserArmoireAlertWindow>())
            Service.WindowManager.AddWindow(new GlamourDresserArmoireAlertWindow(this));
    }
}
