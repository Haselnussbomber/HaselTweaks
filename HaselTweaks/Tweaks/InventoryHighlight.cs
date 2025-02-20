using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public unsafe partial class InventoryHighlight(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    IFramework Framework,
    IClientState ClientState,
    IGameConfig GameConfig,
    IGameGui GameGui,
    IKeyState KeyState,
    IAddonLifecycle AddonLifecycle)
    : IConfigurableTweak
{
    public string InternalName => nameof(InventoryHighlight);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private uint ItemInventryWindowSizeType = 0;
    private uint ItemInventryRetainerWindowSizeType = 0;
    private uint HoveredItemId;
    private bool WasHighlighting;

    public void OnInitialize() { }

    public void OnEnable()
    {
        Framework.Update += OnFrameworkUpdate;
        ClientState.Login += UpdateItemInventryWindowSizeTypes;
        GameConfig.UiConfigChanged += GameConfig_UiConfigChanged;

        AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailPostRequestedUpdate);
        UpdateItemInventryWindowSizeTypes();
    }

    public void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;
        ClientState.Login -= UpdateItemInventryWindowSizeTypes;
        GameConfig.UiConfigChanged -= GameConfig_UiConfigChanged;

        AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailPostRequestedUpdate);

        if (Status is TweakStatus.Enabled)
            ResetGrids();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void GameConfig_UiConfigChanged(object? sender, ConfigChangeEvent evt)
    {
        if (evt.Option is UiConfigOption uiConfigOption && uiConfigOption is UiConfigOption.ItemInventryWindowSizeType or UiConfigOption.ItemInventryRetainerWindowSizeType)
        {
            UpdateItemInventryWindowSizeTypes();
            ResetGrids();
        }
    }

    private void UpdateItemInventryWindowSizeTypes()
    {
        GameConfig.TryGet(UiConfigOption.ItemInventryWindowSizeType, out ItemInventryWindowSizeType);
        GameConfig.TryGet(UiConfigOption.ItemInventryRetainerWindowSizeType, out ItemInventryRetainerWindowSizeType);
    }

    private void OnItemDetailPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (IsHighlightActive())
        {
            var addon = (AtkUnitBase*)args.Addon;
            if (addon->IsVisible)
                addon->IsVisible = false;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        HoveredItemId = NormalizeItemId((uint)GameGui.HoveredItem);

        if (IsHighlightActive())
        {
            if (TryGetAddon<AtkUnitBase>("ItemDetail", out var addonItemDetail) && addonItemDetail->IsVisible)
                addonItemDetail->IsVisible = false;

            var duplicateItemIds = GetDuplicateItemIds();

            // TODO: highlight tabs?!

            HighlightInInventory(duplicateItemIds);
            HighlightInInventoryBuddy(duplicateItemIds);
            HighlightInRetainerInventory(duplicateItemIds);

            if (!WasHighlighting)
                WasHighlighting = true;
        }
        else if (WasHighlighting)
        {
            if (TryGetAddon<AtkUnitBase>("ItemDetail", out var addonItemDetail))
                addonItemDetail->IsVisible = true;

            ResetGrids();

            WasHighlighting = false;
        }
    }

    private HashSet<uint> GetDuplicateItemIds()
    {
        var set = new HashSet<uint>();
        var dupeIds = new HashSet<uint>();

        void ProcessInventoryType(InventoryType type)
        {
            var container = InventoryManager.Instance()->GetInventoryContainer(type);
            if (container->Loaded == 0)
                return;

            for (var i = 0; i < container->Size; i++)
            {
                var slot = container->GetInventorySlot(i);
                if (slot == null)
                    continue;

                var itemId = NormalizeItemId(slot->GetItemId());

                if (!set.Add(itemId))
                    dupeIds.Add(itemId);
            }
        }

        if (IsAddonOpen("Inventory") || IsAddonOpen("InventoryLarge") || IsAddonOpen("InventoryExpansion"))
        {
            ProcessInventoryType(InventoryType.Inventory1);
            ProcessInventoryType(InventoryType.Inventory2);
            ProcessInventoryType(InventoryType.Inventory3);
            ProcessInventoryType(InventoryType.Inventory4);
        }

        if (IsAddonOpen("InventoryBuddy"))
        {
            ProcessInventoryType(InventoryType.SaddleBag1);
            ProcessInventoryType(InventoryType.SaddleBag2);
            ProcessInventoryType(InventoryType.PremiumSaddleBag1);
            ProcessInventoryType(InventoryType.PremiumSaddleBag2);
        }

        if (IsAddonOpen("InventoryRetainer") || IsAddonOpen("InventoryRetainerLarge"))
        {
            ProcessInventoryType(InventoryType.RetainerPage1);
            ProcessInventoryType(InventoryType.RetainerPage2);
            ProcessInventoryType(InventoryType.RetainerPage3);
            ProcessInventoryType(InventoryType.RetainerPage4);
            ProcessInventoryType(InventoryType.RetainerPage5);
            ProcessInventoryType(InventoryType.RetainerPage6);
            ProcessInventoryType(InventoryType.RetainerPage7);
        }

        return dupeIds;
    }

    private bool IsHighlightActive()
    {
        if (!KeyState[VirtualKey.SHIFT])
            return false;

        if (IsAddonOpen("Inventory"))
            return true;

        if (IsAddonOpen("InventoryLarge"))
            return true;

        if (IsAddonOpen("InventoryExpansion"))
            return true;

        if (IsAddonOpen("InventoryBuddy"))
            return true;

        if (IsAddonOpen("InventoryRetainer"))
            return true;

        if (IsAddonOpen("InventoryRetainerLarge"))
            return true;

        return false;
    }

    private void HighlightInInventory(HashSet<uint> duplicateItemIds)
    {
        if (!(IsAddonOpen("Inventory") || IsAddonOpen("InventoryLarge") || IsAddonOpen("InventoryExpansion")))
            return;

        var sorter = UIModule.Instance()->GetItemOrderModule()->InventorySorter;
        if (sorter == null || sorter->SortFunctionIndex != -1)
            return;

        for (var i = 0u; i < sorter->Items.LongCount; i++)
        {
            var item = sorter->Items[i].Value;
            if (item == null)
                continue;

            var inventorySlot = GetInventoryItem(sorter, item);
            if (inventorySlot == null)
                continue;

            var itemId = NormalizeItemId(inventorySlot->GetItemId());

            if (itemId == 0 || itemId == HoveredItemId)
            {
                HighlightInventoryItem(item, 100);
            }
            else if (duplicateItemIds.Contains(itemId))
            {
                HighlightInventoryItem(item, 66);
            }
            else
            {
                HighlightInventoryItem(item, 25);
            }
        }
    }

    private void HighlightInventoryItem(ItemOrderModuleSorterItemEntry* item, byte brightness)
    {
        if (!TryGetAddon<AddonInventoryGrid>("InventoryGrid" + item->Page.ToString() + (ItemInventryWindowSizeType == 2 ? "E" : ""), out var addon))
            return;

        if (addon->Slots.Length <= item->Slot)
            return;

        HighlightComponentDragDrop(addon->Slots[item->Slot], brightness);
    }

    private void HighlightInInventoryBuddy(HashSet<uint> duplicateItemIds)
    {
        if (!TryGetAddon<AddonInventoryBuddy>("InventoryBuddy", out var addon))
            return;

        if (addon->TabIndex == 1 && !PlayerState.Instance()->HasPremiumSaddlebag)
            return;

        var sorter = addon->TabIndex == 0
            ? UIModule.Instance()->GetItemOrderModule()->SaddleBagSorter
            : UIModule.Instance()->GetItemOrderModule()->PremiumSaddleBagSorter;

        if (sorter == null || sorter->SortFunctionIndex != -1)
            return;

        for (var i = 0u; i < sorter->Items.LongCount; i++)
        {
            var item = sorter->Items[i].Value;
            if (item == null)
                continue;

            var slotIndex = (int)GetSlotIndex(sorter, item);
            if (addon->Slots.Length <= slotIndex)
                continue;

            var slotComponent = addon->Slots[slotIndex].Value;
            if (slotComponent == null)
                continue;

            var inventorySlot = GetInventoryItem(sorter, slotIndex);
            if (inventorySlot == null)
                continue;

            var itemId = NormalizeItemId(inventorySlot->GetItemId());

            if (itemId == 0 || itemId == HoveredItemId)
            {
                HighlightComponentDragDrop(slotComponent, 100);
            }
            else if (duplicateItemIds.Contains(itemId))
            {
                HighlightComponentDragDrop(slotComponent, 66);
            }
            else
            {
                HighlightComponentDragDrop(slotComponent, 25);
            }
        }
    }

    private void HighlightInRetainerInventory(HashSet<uint> duplicateItemIds)
    {
        var iom = UIModule.Instance()->GetItemOrderModule();
        var activeRetainerId = iom->ActiveRetainerId;
        if (activeRetainerId == 0)
            return;

        ItemOrderModuleSorter* sorter = null;
        foreach (var (key, value) in iom->RetainerSorter)
        {
            if (key == activeRetainerId)
            {
                sorter = value;
                break;
            }
        }

        if (sorter == null || sorter->SortFunctionIndex != -1)
            return;

        for (var i = 0u; i < sorter->Items.LongCount; i++)
        {
            var item = sorter->Items[i].Value;
            if (item == null)
                continue;

            var inventorySlot = GetInventoryItem(sorter, item);
            if (inventorySlot == null)
                continue;

            var itemId = NormalizeItemId(inventorySlot->GetItemId());

            if (itemId == 0 || itemId == HoveredItemId)
            {
                HighlightRetainerInventoryItem(sorter, item, 100);
            }
            else if (duplicateItemIds.Contains(itemId))
            {
                HighlightRetainerInventoryItem(sorter, item, 66);
            }
            else
            {
                HighlightRetainerInventoryItem(sorter, item, 25);
            }
        }
    }

    private void HighlightRetainerInventoryItem(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry, byte brightness)
    {
        var slotIndex = (int)GetSlotIndex(sorter, entry);
        var adjustedItemsPerPage = 35;
        var adjustedPage = slotIndex / adjustedItemsPerPage;
        var adjustedSlotIndex = slotIndex % adjustedItemsPerPage;

        if (ItemInventryRetainerWindowSizeType == 0 && TryGetAddon<AddonInventoryRetainer>("InventoryRetainer", out var addonInventoryRetainer) && addonInventoryRetainer->TabIndex != adjustedPage)
            return;

        if (!TryGetAddon<AddonInventoryGrid>("RetainerGrid" + (ItemInventryRetainerWindowSizeType == 0 ? "" : adjustedPage.ToString()), out var addon))
            return;

        if (addon->Slots.Length <= adjustedSlotIndex)
            return;

        HighlightComponentDragDrop(addon->Slots[adjustedSlotIndex], brightness);
    }

    private void HighlightComponentDragDrop(AtkComponentDragDrop* component, byte brightness)
    {
        if (component == null)
            return;

        var ownerNode = (AtkResNode*)component->AtkComponentBase.OwnerNode;
        if (ownerNode == null)
            return;

        SetSlotBrightness(ownerNode, brightness);
    }

    private void ResetGrids()
    {
        switch (ItemInventryWindowSizeType)
        {
            case 0: // Inventory
            case 1: // InventoryLarge
                ResetInventoryGrid("InventoryGrid0");
                ResetInventoryGrid("InventoryGrid1");
                ResetInventoryGrid("InventoryGrid2");
                ResetInventoryGrid("InventoryGrid3");
                break;

            case 2: // InventoryExpanded
                ResetInventoryGrid("InventoryGrid0E");
                ResetInventoryGrid("InventoryGrid1E");
                ResetInventoryGrid("InventoryGrid2E");
                ResetInventoryGrid("InventoryGrid3E");
                break;
        }

        if (TryGetAddon<AddonInventoryBuddy>("InventoryBuddy", out var addonInventoryBuddy))
            ResetSlots(addonInventoryBuddy->Slots);

        switch (ItemInventryRetainerWindowSizeType)
        {
            case 0: // InventoryRetainer
                ResetInventoryGrid("RetainerGrid");
                break;

            case 1: // InventoryRetainerLarge
                ResetInventoryGrid("RetainerGrid0");
                ResetInventoryGrid("RetainerGrid1");
                ResetInventoryGrid("RetainerGrid2");
                ResetInventoryGrid("RetainerGrid3");
                ResetInventoryGrid("RetainerGrid4");
                break;
        }
    }

    private void ResetInventoryGrid(string addonName)
    {
        if (TryGetAddon<AddonInventoryGrid>(addonName, out var addon))
            ResetSlots(addon->Slots);
    }

    private void ResetSlots(Span<Pointer<AtkComponentDragDrop>> SlotsSpan)
    {
        foreach (AtkComponentDragDrop* component in SlotsSpan)
        {
            if (component == null)
                continue;

            SetSlotBrightness((AtkResNode*)component->AtkComponentBase.OwnerNode, 100);
        }
    }

    private void SetSlotBrightness(AtkResNode* node, byte brightness)
    {
        if (node == null/* || !node->IsVisible*/)
            return;

        node->MultiplyRed = brightness;
        node->MultiplyGreen = brightness;
        node->MultiplyBlue = brightness;
    }

    private long GetSlotIndex(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry)
        => entry->Slot + sorter->ItemsPerPage * entry->Page;

    private InventoryItem* GetInventoryItem(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry)
        => GetInventoryItem(sorter, GetSlotIndex(sorter, entry));

    private InventoryItem* GetInventoryItem(ItemOrderModuleSorter* sorter, long slotIndex)
    {
        if (sorter == null)
            return null;

        if (sorter->Items.LongCount <= slotIndex)
            return null;

        var item = sorter->Items[slotIndex].Value;
        if (item == null)
            return null;

        var container = InventoryManager.Instance()->GetInventoryContainer(sorter->InventoryType + item->Page);
        if (container == null)
            return null;

        return container->GetInventorySlot(item->Slot);
    }

    private uint NormalizeItemId(ExcelRowId<Item> itemId)
        => Config.IgnoreQuality
            ? itemId.GetBaseId()
            : itemId;
}
