using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using HaselTweaks.Structs.Addons;

namespace HaselTweaks.Tweaks;

public class InventoryHighlightConfiguration
{
    [BoolConfig]
    public bool IgnoreQuality = true;
}

[Tweak]
public unsafe class InventoryHighlight : Tweak<InventoryHighlightConfiguration>
{
    private uint ItemInventryWindowSizeType = 0;
    private uint ItemInventryRetainerWindowSizeType = 0;
    private uint HoveredItemId;
    private bool WasHighlighting;

    public override void Enable()
    {
        Service.GameConfig.UiConfigChanged += GameConfig_UiConfigChanged;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailPostRequestedUpdate);
        UpdateItemInventryWindowSizeTypes();
    }

    public override void Disable()
    {
        Service.GameConfig.UiConfigChanged -= GameConfig_UiConfigChanged;
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailPostRequestedUpdate);
        ResetGrids();
    }

    public override void OnLogin() => UpdateItemInventryWindowSizeTypes();

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
        Service.GameConfig.TryGet(UiConfigOption.ItemInventryWindowSizeType, out ItemInventryWindowSizeType);
        Service.GameConfig.TryGet(UiConfigOption.ItemInventryRetainerWindowSizeType, out ItemInventryRetainerWindowSizeType);
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

    public override void OnFrameworkUpdate()
    {
        HoveredItemId = NormalizeItemId((uint)Service.GameGui.HoveredItem);

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
        if (!Service.KeyState[VirtualKey.SHIFT])
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

        for (var i = 0u; i < sorter->Items.Size(); i++)
        {
            var item = sorter->Items.Get(i).Value;
            if (item == null)
                continue;

            var inventorySlot = GetInventoryItem(sorter, item);
            if (inventorySlot == null)
                continue;

            var itemId  = NormalizeItemId(inventorySlot->GetItemId());

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

        if (addon->SlotsSpan.Length <= item->Slot)
            return;

        HighlightComponentDragDrop(addon->SlotsSpan[item->Slot], brightness);
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

        for (var i = 0u; i < sorter->Items.Size(); i++)
        {
            var item = sorter->Items.Get(i).Value;
            if (item == null)
                continue;

            var slotIndex = (int)GetSlotIndex(sorter, item);
            if (addon->SlotsSpan.Length <= slotIndex)
                continue;

            var slotComponent = addon->SlotsSpan[slotIndex].Value;
            if (slotComponent == null)
                continue;

            var inventorySlot = GetInventoryItem(sorter, (ulong)slotIndex);
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

        for (var i = 0u; i < sorter->Items.Size(); i++)
        {
            var item = sorter->Items.Get(i).Value;
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

        if (addon->SlotsSpan.Length <= adjustedSlotIndex)
            return;

        HighlightComponentDragDrop(addon->SlotsSpan[adjustedSlotIndex], brightness);
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
            ResetSlots(addonInventoryBuddy->SlotsSpan);

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
            ResetSlots(addon->SlotsSpan);
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

    private ulong GetSlotIndex(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry)
        => (ulong)(entry->Slot + sorter->ItemsPerPage * entry->Page);

    private InventoryItem* GetInventoryItem(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry)
        => GetInventoryItem(sorter, GetSlotIndex(sorter, entry));

    private InventoryItem* GetInventoryItem(ItemOrderModuleSorter* sorter, ulong slotIndex)
    {
        if (sorter == null)
            return null;

        if (sorter->Items.Size() <= slotIndex)
            return null;

        var item = sorter->Items.Get(slotIndex).Value;
        if (item == null)
            return null;

        var container = InventoryManager.Instance()->GetInventoryContainer(sorter->InventoryType + item->Page);
        if (container == null)
            return null;

        return container->GetInventorySlot(item->Slot);
    }

    private uint NormalizeItemId(uint itemId)
    {
        if (Config.IgnoreQuality && ItemUtils.IsHighQuality(itemId))
            itemId -= 1_000_000;

        return itemId;
    }
}
