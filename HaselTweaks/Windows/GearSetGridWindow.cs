using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using GearsetEntry = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetEntry;
using GearsetFlag = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetFlag;
using GearsetItem = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetItem;

namespace HaselTweaks.Windows;

public unsafe class GearSetGridWindow : LockableWindow
{
    private static readonly Vector2 IconSize = new(34);
    private static readonly Vector2 IconInset = IconSize * 0.08333f;
    private static readonly float ItemCellWidth = IconSize.X;
    private readonly IClientState ClientState;
    private readonly TextureService TextureService;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly ImGuiContextMenuService ImGuiContextMenuService;
    private readonly ItemService ItemService;
    private bool _resetScrollPosition;

    public GearSetGridConfiguration Config => PluginConfig.Tweaks.GearSetGrid;

    public GearSetGridWindow(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        IClientState clientState,
        TextureService textureService,
        ExcelService excelService,
        TextService textService,
        ImGuiContextMenuService imGuiContextMenuService,
        ItemService itemService)
        : base(windowManager, pluginConfig, textService, textService.Translate("GearSetGridWindow.Title"))
    {
        ClientState = clientState;
        TextureService = textureService;
        ExcelService = excelService;
        TextService = textService;
        ImGuiContextMenuService = imGuiContextMenuService;
        ItemService = itemService;

        DisableWindowSounds = Config.AutoOpenWithGearSetList;

        Flags |= ImGuiWindowFlags.NoCollapse;

        Size = new(505, 550);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(505, 200),
            MaximumSize = new Vector2(4096),
        };
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _resetScrollPosition = true;
    }

    public override bool DrawConditions()
        => ClientState.IsLoggedIn;

    public override void Draw()
    {
        const int NUM_SLOTS = 13;

        using var padding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero)
                                  .Push(ImGuiStyleVar.FramePadding, Vector2.Zero);

        using var table = ImRaii.Table("##Table", 1 + NUM_SLOTS, ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
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
        var gearsetCount = InventoryManager.Instance()->GetPermittedGearsetCount();
        for (var gearsetIndex = 0; gearsetIndex < gearsetCount; gearsetIndex++)
        {
            var gearset = raptureGearsetModule->GetGearset(gearsetIndex);
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists))
                continue;

            using var id = ImRaii.PushId($"Gearset_{gearsetIndex}");

            var name = gearset->NameString;

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
                    ImGui.TextUnformatted($"[{gearset->Id + 1}] {name}");

                    if (gearset->GlamourSetLink != 0)
                    {
                        ImGui.TextUnformatted($"{TextService.GetAddonText(3185)}: {gearset->GlamourSetLink}"); // "Glamour Plate: {link}"
                    }
                }

                ImGuiContextMenuService.Draw("##GearsetContext", builder =>
                {
                    builder
                        .AddGearsetLinkGlamour(gearset)
                        .AddGearsetChangeGlamour(gearset)
                        .AddGearsetUnlinkGlamour(gearset)
                        .AddGearsetChangePortrait(gearset);
                });

                var iconSize = 28 * ImGuiHelpers.GlobalScale;
                var itemStartPos = startPos + new Vector2(region.X / 2f - iconSize / 2f, ImGui.GetStyle().ItemInnerSpacing.Y); // start from the right

                // class icon
                ImGui.SetCursorPos(itemStartPos);
                TextureService.DrawIcon(62100 + gearset->ClassJob, iconSize);

                // gearset number
                var text = $"{gearsetIndex + 1}";
                ImGui.SetCursorPos(itemStartPos + new Vector2(iconSize / 2f - ImGui.CalcTextSize(text).X / 2f, iconSize));
                ImGui.TextUnformatted(text);
            }

            for (var slotIndex = 0u; slotIndex < NUM_SLOTS; slotIndex++)
            {
                // skip obsolete belt slot
                if (slotIndex == 5)
                    continue;

                var slot = gearset->Items.GetPointer((int)slotIndex);
                var itemId = slot->ItemId % 1000000; // strip HQ

                ImGui.TableNextColumn();

                if (itemId == 0)
                {
                    var windowPos = ImGui.GetWindowPos();
                    var cursorPos = ImGui.GetCursorPos();

                    // icon background
                    ImGui.SetCursorPos(cursorPos);
                    TextureService.DrawPart("Character", 8, 0, IconSize * ImGuiHelpers.GlobalScale);

                    ImGui.SetCursorPos(cursorPos + IconInset * ImGuiHelpers.GlobalScale);
                    var iconIndex = slotIndex switch
                    {
                        12 => 11u, // left ring
                        _ => slotIndex,
                    };
                    TextureService.DrawPart("Character", 12, 17 + iconIndex, (IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

                    continue;
                }

                var item = ExcelService.GetRow<Item>(itemId);
                if (item == null)
                    continue;

                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                DrawItemIcon(gearset, slotIndex, slot, item, $"GearsetItem_{gearsetIndex}_{slotIndex}");

                var itemLevelText = $"{item.LevelItem.Row}";
                ImGuiUtils.PushCursorX(IconSize.X * ImGuiHelpers.GlobalScale / 2f - ImGui.CalcTextSize(itemLevelText).X / 2f);
                ImGuiUtils.TextUnformattedColored(ItemService.GetItemLevelColor(gearset->ClassJob, item, Colors.Red, Colors.Yellow, Colors.Green), itemLevelText);

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
            var gearset = raptureGearsetModule->GetGearset(gearsetIndex);
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists))
                continue;

            var item = gearset->Items.GetPointer((int)slotIndex);
            if (item->ItemId == itemId)
                list.Add(new(gearset->Id, gearset->NameString));
        }

        return list;
    }

    public void DrawItemIcon(GearsetEntry* gearset, uint slotIndex, GearsetItem* slot, Item item, string key)
    {
        var popupKey = $"##ItemContextMenu_{key}_{item.RowId}_Tooltip";

        //var isEventItem = slot.ItemID > 2000000;
        //var isCollectable = slot.ItemID is > 500000 and < 1000000;
        var isHq = slot->ItemId is > 1000000 and < 1500000;

        var startPos = ImGui.GetCursorPos();

        // icon background
        ImGui.SetCursorPos(startPos);
        TextureService.DrawPart("Character", 7, 4, IconSize * ImGuiHelpers.GlobalScale);

        // icon
        ImGui.SetCursorPos(startPos + IconInset * ImGuiHelpers.GlobalScale);
        TextureService.DrawIcon(new GameIconLookup(item.Icon, isHq), (IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

        // icon overlay
        ImGui.SetCursorPos(startPos);
        TextureService.DrawPart("Character", 7, 0, IconSize * ImGuiHelpers.GlobalScale);

        // icon hover effect
        if (ImGui.IsItemHovered() || ImGui.IsPopupOpen(popupKey))
        {
            ImGui.SetCursorPos(startPos);
            TextureService.DrawPart("Character", 7, 5, IconSize * ImGuiHelpers.GlobalScale);
        }

        ImGui.SetCursorPos(startPos + new Vector2(0, (IconSize.Y - 3) * ImGuiHelpers.GlobalScale));

        ImGuiContextMenuService.Draw(popupKey, builder =>
        {
            builder
                .AddTryOn(item, slot->GlamourId, slot->Stain0Id, slot->Stain1Id)
                .AddItemFinder(item.RowId)
                .AddCopyItemName(item.RowId)
                .AddOpenOnGarlandTools("item", item.RowId)
                .AddItemSearch(item);
        });

        if (!ImGui.IsItemHovered())
            return;

        using var _ = ImRaii.Tooltip();

        ImGuiUtils.TextUnformattedColored(ItemService.GetItemRarityColor(item), TextService.GetItemName(item.RowId));

        var holdingShift = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
        if (holdingShift)
        {
            ImGuiUtils.SameLineSpace();
            ImGui.TextUnformatted($"[{item.RowId}]");
        }

        if (item.ItemUICategory.Row != 0)
        {
            ImGuiUtils.PushCursorY(-ImGui.GetStyle().ItemSpacing.Y);
            ImGui.TextUnformatted(ExcelService.GetRow<ItemUICategory>(item.ItemUICategory.Row)?.Name.ExtractText() ?? string.Empty);
        }

        if (slot->GlamourId != 0 || slot->Stain0Id != 0 || slot->Stain1Id != 0)
            ImGuiUtils.DrawPaddedSeparator();

        if (slot->GlamourId != 0)
        {
            TextService.Draw("GearSetGridWindow.ItemTooltip.LabelGlamour");
            var glamourItem = ExcelService.GetRow<Item>(slot->GlamourId)!;
            ImGuiUtils.SameLineSpace();
            ImGuiUtils.TextUnformattedColored(ItemService.GetItemRarityColor(glamourItem), TextService.GetItemName(slot->GlamourId));

            if (holdingShift)
            {
                ImGuiUtils.SameLineSpace();
                ImGui.TextUnformatted($"[{slot->GlamourId}]");
            }
        }

        if (slot->Stain0Id != 0)
        {
            TextService.Draw("GearSetGridWindow.ItemTooltip.LabelDye0");
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)ExcelService.GetRow<Stain>(slot->Stain0Id)!.GetColor()))
                ImGui.Bullet();
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(ExcelService.GetRow<Stain>(slot->Stain0Id)!.Name.ExtractText().FirstCharToUpper());

            if (holdingShift)
            {
                ImGuiUtils.SameLineSpace();
                ImGui.TextUnformatted($"[{slot->Stain0Id}]");
            }
        }

        if (slot->Stain1Id != 0)
        {
            TextService.Draw("GearSetGridWindow.ItemTooltip.LabelDye1");
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)ExcelService.GetRow<Stain>(slot->Stain1Id)!.GetColor()))
                ImGui.Bullet();
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(ExcelService.GetRow<Stain>(slot->Stain1Id)!.Name.ExtractText().FirstCharToUpper());

            if (holdingShift)
            {
                ImGuiUtils.SameLineSpace();
                ImGui.TextUnformatted($"[{slot->Stain1Id}]");
            }
        }

        var usedInGearsets = GetItemInGearsetsList(slot->ItemId, slotIndex);
        if (usedInGearsets.Count > 1)
        {
            ImGuiUtils.DrawPaddedSeparator();
            TextService.Draw("GearSetGridWindow.ItemTooltip.AlsoUsedInTheseGearsets");
            using (ImRaii.PushIndent(ImGui.GetStyle().ItemSpacing.X))
            {
                foreach (var entry in usedInGearsets)
                {
                    if (entry.Id == gearset->Id)
                        continue;

                    ImGui.TextUnformatted($"[{entry.Id + 1}] {entry.Name}");
                }
            }
        }
    }
}
