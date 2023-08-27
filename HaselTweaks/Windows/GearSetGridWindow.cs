using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Utils;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using GearsetEntry = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetEntry;
using GearsetFlag = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetFlag;
using GearsetItem = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetItem;

namespace HaselTweaks.Windows;

public unsafe class GearSetGridWindow : Window
{
    private static readonly Vector2 DefaultWindowSize = new(505, 550);
    private static readonly Vector2 IconSize = new(34);
    private static readonly Vector2 IconInset = IconSize * 0.08333f;
    private static readonly float ItemCellWidth = IconSize.X;
    public static GearSetGrid.Configuration Config => Plugin.Config.Tweaks.GearSetGrid;

    private bool _resetSize;
    private bool _resetScrollPosition;

    public GearSetGridWindow() : base(t("GearSetGridWindow.Title"))
    {
        base.Namespace = "HaselTweaks_GearSetGrid";
        base.DisableWindowSounds = Config.AutoOpenWithGearSetList;
        base.IsOpen = true;

        base.Flags |= ImGuiWindowFlags.NoCollapse;

        if (Plugin.Config.LockedImGuiWindows.Contains(nameof(GearSetGridWindow)))
            base.Flags |= ImGuiWindowFlags.NoMove;

        base.Size = DefaultWindowSize;
        base.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        _resetScrollPosition = true;
    }

    public override void PreDraw()
    {
        if (_resetSize)
        {
            base.Size = DefaultWindowSize;
            base.SizeCondition = ImGuiCond.Always;
            _resetSize = false;
        }
        else
        {
            base.SizeCondition = ImGuiCond.FirstUseEver;
        }
    }

    public override void Draw()
    {
        using (var windowContext = ImRaii.ContextPopup(nameof(GearSetGridWindow)))
        {
            if (windowContext.Success)
            {
                if (ImGui.MenuItem(t("ImGuiWindow.ContextMenuItem.ResetSize")))
                {
                    _resetSize = true;
                }

                if (ImGui.MenuItem(
                    t("ImGuiWindow.ContextMenuItem.LockPosition"),
                    null,
                    base.Flags.HasFlag(ImGuiWindowFlags.NoMove)))
                {
                    base.Flags ^= ImGuiWindowFlags.NoMove;

                    if (base.Flags.HasFlag(ImGuiWindowFlags.NoMove))
                    {
                        if (!Plugin.Config.LockedImGuiWindows.Contains(nameof(GearSetGridWindow)))
                        {
                            Plugin.Config.LockedImGuiWindows.Add(nameof(GearSetGridWindow));
                            Plugin.Config.Save();
                        }
                    }
                    else
                    {
                        if (Plugin.Config.LockedImGuiWindows.Contains(nameof(GearSetGridWindow)))
                        {
                            Plugin.Config.LockedImGuiWindows.Remove(nameof(GearSetGridWindow));
                            Plugin.Config.Save();
                        }
                    }
                }
            }
        }

        const int NUM_SLOTS = 13;

        using var cellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero);
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var table = ImRaii.Table("##Table", 1 + NUM_SLOTS, ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table.Success)
            return;

        if (_resetScrollPosition)
        {
            ImGui.SetScrollY(0);
            _resetScrollPosition = false;
        }

        ImGui.TableSetupColumn("##ID", ImGuiTableColumnFlags.WidthFixed, (24 + 14 + ImGui.GetStyle().ItemInnerSpacing.X * 2f + ImGui.GetStyle().ItemSpacing.X * 2f) * ImGuiHelpers.GlobalScale);

        for (var slotIndex = 0; slotIndex < NUM_SLOTS; slotIndex++)
        {
            // skip obsolete belt slot
            if (slotIndex == 5)
                continue;

            ImGui.TableSetupColumn($"##Slot{slotIndex}", ImGuiTableColumnFlags.WidthFixed, IconSize.X * ImGuiHelpers.GlobalScale);
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        for (var gearsetIndex = 0; gearsetIndex < InventoryManager.Instance()->GetPermittedGearsetCount(); gearsetIndex++)
        {
            var gearset = raptureGearsetModule->Gearset[gearsetIndex];
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists))
                continue;

            using var id = ImRaii.PushId($"Gearset_{gearsetIndex}");

            var name = MemoryHelper.ReadString((nint)gearset->Name, 0x2F);

            if (Config.ConvertSeparators && name == Config.SeparatorFilter)
            {
                if (!Config.DisableSeparatorSpacing)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Dummy(new Vector2(1, IconSize.Y * ImGuiHelpers.GlobalScale / 2f));
                }
                continue;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            {
                var startPos = ImGui.GetCursorPos();
                var region = ImGui.GetContentRegionAvail();
                var rowHeight = IconSize.Y * ImGuiHelpers.GlobalScale + ImGui.GetFrameHeight();

                if (ImGui.Selectable("##Equip", gearsetIndex == raptureGearsetModule->CurrentGearsetIndex, ImGuiSelectableFlags.None, new Vector2(region.X, rowHeight)))
                {
                    UIModule.PlaySound(8, 0, 0, 0);
                    raptureGearsetModule->EquipGearset(gearsetIndex);
                }
                if (ImGui.IsItemHovered())
                {
                    using var tooltip = ImRaii.Tooltip();
                    ImGui.TextUnformatted($"[{gearset->ID + 1}] {name}");

                    if (gearset->GlamourSetLink != 0)
                    {
                        ImGui.TextUnformatted($"{GetAddonText(3185)}: {gearset->GlamourSetLink}"); // "Glamour Plate: {link}"
                    }
                }

                new ImGuiContextMenu("##GearsetContext")
                {
                    ImGuiContextMenu.CreateGearsetLinkGlamour(gearset->ID),
                    ImGuiContextMenu.CreateGearsetChangeGlamour(gearset->ID),
                    ImGuiContextMenu.CreateGearsetUnlinkGlamour(gearset->ID),
                    ImGuiContextMenu.CreateGearsetChangePortrait(gearset->ID)
                }
                .Draw();

                var iconSize = 28 * ImGuiHelpers.GlobalScale;
                var itemStartPos = startPos + new Vector2(region.X / 2f - iconSize / 2f, ImGui.GetStyle().ItemInnerSpacing.Y); // start from the right

                // class icon
                ImGui.SetCursorPos(itemStartPos);
                Service.TextureManager.GetIcon(62100 + gearset->ClassJob).Draw(iconSize);

                // gearset number
                var text = $"{gearsetIndex + 1}";
                ImGui.SetCursorPos(itemStartPos + new Vector2(iconSize / 2f - ImGui.CalcTextSize(text).X / 2f, iconSize));
                ImGui.TextUnformatted(text);
            }

            for (uint slotIndex = 0; slotIndex < NUM_SLOTS; slotIndex++)
            {
                // skip obsolete belt slot
                if (slotIndex == 5)
                    continue;

                var slot = (GearsetItem*)((nint)gearset->ItemsData + GearsetItem.Size * slotIndex);
                var itemId = slot->ItemID % 1000000; // strip HQ

                ImGui.TableNextColumn();

                if (itemId == 0)
                {
                    var windowPos = ImGui.GetWindowPos();
                    var cursorPos = ImGui.GetCursorPos();

                    // icon background
                    ImGui.SetCursorPos(cursorPos);
                    Service.TextureManager
                        .GetPart("Character", 8, 0)
                        .Draw(IconSize * ImGuiHelpers.GlobalScale);

                    ImGui.SetCursorPos(cursorPos + IconInset * ImGuiHelpers.GlobalScale);
                    Service.TextureManager
                        .GetPart("Character", 11, 17 + slotIndex)
                        .Draw((IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

                    continue;
                }

                var item = GetRow<Item>(itemId);
                if (item == null)
                    continue;

                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                DrawItemIcon(gearset, slotIndex, slot, item, $"GearsetItem_{gearsetIndex}_{slotIndex}");

                var itemLevelText = $"{item.LevelItem.Row}";
                ImGuiUtils.PushCursorX(IconSize.X * ImGuiHelpers.GlobalScale / 2f - ImGui.CalcTextSize(itemLevelText).X / 2f);
                ImGuiUtils.TextUnformattedColored(Colors.GetItemLevelColor(gearset->ClassJob, item, Colors.Red, Colors.Yellow, Colors.Green), itemLevelText);

                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);
            }
        }
    }

    private record ItemInGearsetEntry(byte Id, string Name);
    private List<ItemInGearsetEntry> GetItemInGearsetsList(uint itemId, uint slotIndex)
    {
        var list = new List<ItemInGearsetEntry>();

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        for (var gearsetIndex = 0; gearsetIndex < InventoryManager.Instance()->GetPermittedGearsetCount(); gearsetIndex++)
        {
            var gearset = raptureGearsetModule->Gearset[gearsetIndex];
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists))
                continue;

            var item = (GearsetItem*)((nint)gearset->ItemsData + GearsetItem.Size * slotIndex);
            if (item->ItemID == itemId)
                list.Add(new(gearset->ID, MemoryHelper.ReadString((nint)gearset->Name, 0x2F)));
        }

        return list;
    }

    public void DrawItemIcon(GearsetEntry* gearset, uint slotIndex, GearsetItem* slot, Item item, string key)
    {
        var popupKey = $"##ItemContextMenu_{key}_{item.RowId}_Tooltip";

        //var isEventItem = slot.ItemID > 2000000;
        //var isCollectable = slot.ItemID is > 500000 and < 1000000;
        var isHq = slot->ItemID is > 1000000 and < 1500000;

        var startPos = ImGui.GetCursorPos();

        // icon background
        ImGui.SetCursorPos(startPos);
        Service.TextureManager
            .GetPart("Character", 7, 4)
            .Draw(IconSize * ImGuiHelpers.GlobalScale);

        // icon
        ImGui.SetCursorPos(startPos + IconInset * ImGuiHelpers.GlobalScale);
        Service.TextureManager
            .GetIcon(item.Icon, isHq)
            .Draw((IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

        // icon overlay
        ImGui.SetCursorPos(startPos);
        Service.TextureManager
            .GetPart("Character", 7, 0)
            .Draw(IconSize * ImGuiHelpers.GlobalScale);

        // icon hover effect
        if (ImGui.IsItemHovered() || ImGui.IsPopupOpen(popupKey))
        {
            ImGui.SetCursorPos(startPos);
            Service.TextureManager
                .GetPart("Character", 7, 5)
                .Draw(IconSize * ImGuiHelpers.GlobalScale);
        }

        ImGui.SetCursorPos(startPos + new Vector2(0, (IconSize.Y - 3) * ImGuiHelpers.GlobalScale));

        new ImGuiContextMenu(popupKey)
        {
            ImGuiContextMenu.CreateTryOn(item.RowId, slot->GlamourId, slot->Stain),
            ImGuiContextMenu.CreateItemFinder(item.RowId),
            ImGuiContextMenu.CreateCopyItemName(item.RowId),
            ImGuiContextMenu.CreateOpenOnGarlandTools(item.RowId),
            ImGuiContextMenu.CreateItemSearch(item.RowId),
        }
        .Draw();

        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                ImGuiUtils.TextUnformattedColored(Colors.GetItemRarityColor(item.Rarity), GetRow<Item>(item.RowId)!.Singular.ToDalamudString().ToString());

                var holdingShift = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
                if (holdingShift)
                {
                    ImGuiUtils.SameLineSpace();
                    ImGui.TextUnformatted($"[{item.RowId}]");
                }

                if (item.ItemUICategory.Row != 0)
                {
                    ImGuiUtils.PushCursorY(-ImGui.GetStyle().ItemSpacing.Y);
                    ImGui.TextUnformatted(GetSheetText<ItemUICategory>(item.ItemUICategory.Row, "Name"));
                }

                if (slot->GlamourId != 0 || slot->Stain != 0)
                    ImGuiUtils.DrawPaddedSeparator();

                if (slot->GlamourId != 0)
                {
                    ImGui.TextUnformatted(t("GearSetGridWindow.ItemTooltip.LabelGlamour"));
                    var glamourItem = GetRow<Item>(slot->GlamourId)!;
                    ImGuiUtils.SameLineSpace();
                    ImGuiUtils.TextUnformattedColored(Colors.GetItemRarityColor(glamourItem.Rarity), GetRow<Item>(slot->GlamourId)!.Singular.ToDalamudString().ToString());

                    if (holdingShift)
                    {
                        ImGuiUtils.SameLineSpace();
                        ImGui.TextUnformatted($"[{slot->GlamourId}]");
                    }
                }

                if (slot->Stain != 0)
                {
                    ImGui.TextUnformatted(t("GearSetGridWindow.ItemTooltip.LabelDye"));
                    ImGuiUtils.SameLineSpace();
                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.GetStainColor(slot->Stain)))
                        ImGui.Bullet();
                    ImGui.SameLine(0, 0);
                    ImGui.TextUnformatted(GetSheetText<Stain>(slot->Stain, "Name"));

                    if (holdingShift)
                    {
                        ImGuiUtils.SameLineSpace();
                        ImGui.TextUnformatted($"[{slot->Stain}]");
                    }
                }

                var usedInGearsets = GetItemInGearsetsList(slot->ItemID, slotIndex);
                if (usedInGearsets.Count > 1)
                {
                    ImGuiUtils.DrawPaddedSeparator();
                    ImGui.TextUnformatted(t("GearSetGridWindow.ItemTooltip.AlsoUsedInTheseGearsets"));
                    using (ImRaii.PushIndent(ImGui.GetStyle().ItemSpacing.X))
                    {
                        foreach (var entry in usedInGearsets)
                        {
                            if (entry.Id == gearset->ID)
                                continue;

                            ImGui.TextUnformatted($"[{entry.Id + 1}] {entry.Name}");
                        }
                    }
                }
            }
        }
    }
}
