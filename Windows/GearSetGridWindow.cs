using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Caches;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using GearsetFlag = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetFlag;
using GearsetItem = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetItem;
using Item = HaselTweaks.Sheets.Item;

namespace HaselTweaks.Windows;

public unsafe class GearSetGridWindow : Window
{
    private static readonly Vector2 IconSize = new(34);
    private static readonly Vector2 IconInset = IconSize * 0.08333f;
    private static readonly float ItemCellWidth = IconSize.X;
    public static GearSetGrid.Configuration Config => Plugin.Config.Tweaks.GearSetGrid;

    private readonly GearSetGrid _tweak;
    private bool _resetScrollPosition;

    public GearSetGridWindow(GearSetGrid tweak) : base("[HaselTweaks] Gear Set Grid")
    {
        _tweak = tweak;

        base.Namespace = "HaselTweaks_GearSetGrid";
        base.DisableWindowSounds = true;
        base.IsOpen = true;

        Size = new(515, 554);
    }

    public override void OnOpen()
    {
        _resetScrollPosition = true;
    }

    public override void Draw()
    {
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

            using var id = ImRaii.PushId($"Gearset#{gearsetIndex}");

            var name = MemoryHelper.ReadStringNullTerminated((nint)gearset->Name);

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

                if (Config.AllowSwitchingGearsets)
                {
                    if (ImGui.Selectable($"##Equip", gearsetIndex == raptureGearsetModule->CurrentGearsetIndex, ImGuiSelectableFlags.None, new Vector2(region.X, rowHeight)))
                    {
                        UIModule.PlaySound(8, 0, 0, 0);
                        raptureGearsetModule->EquipGearset(gearsetIndex);
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    }
                }

                ImGui.SetCursorPos(startPos);
                ImGuiUtils.PushCursor(ImGui.GetStyle().ItemSpacing.X, (rowHeight - ImGui.GetFrameHeight()) / 2f);
                var gearsetNumber = $"{gearsetIndex + 1}";
                var textSize = ImGui.CalcTextSize(gearsetNumber);

                ImGuiUtils.PushCursorX(region.X / 2f - textSize.X - ImGui.GetStyle().ItemSpacing.X);
                ImGui.Text(gearsetNumber);

                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
                Service.TextureCache.GetIcon(62000 + gearset->ClassJob).Draw(20 * ImGuiHelpers.GlobalScale);
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
                    Service.TextureCache
                        .GetPart("Character", 8, 0)
                        .Draw(IconSize * ImGuiHelpers.GlobalScale);

                    ImGui.SetCursorPos(cursorPos + IconInset * ImGuiHelpers.GlobalScale);
                    Service.TextureCache
                        .GetPart("Character", 11, 17 + slotIndex)
                        .Draw((IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

                    continue;
                }

                var item = Service.Data.GetExcelSheet<Item>()!.GetRow(itemId);
                if (item == null)
                    continue;

                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                DrawItemIcon(slot, item, $"GearsetItem_{gearsetIndex}_{slotIndex}");

                if (ImGui.IsItemHovered())
                {
                    using (ImRaii.Tooltip())
                    {
                        ImGui.TextColored(Colors.GetItemRarityColor(item.Rarity), StringCache.GetItemName(itemId));

                        if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                        {
                            ImGuiUtils.DrawPaddedSeparator();
                            ImGui.Text($"Slot: {StringCache.GetAddonText(738 + slotIndex)} ({slotIndex})");
                            ImGui.Text($"ItemID: {slot->ItemID}");
                            ImGui.Text($"GlamourID: {slot->GlamourId}");
                            if (slot->GlamourId != 0)
                            {
                                var glamourItem = Service.Data.GetExcelSheet<Item>()!.GetRow(slot->GlamourId)!;
                                ImGuiUtils.SameLineSpace();
                                ImGui.Text("(");
                                ImGui.SameLine(0, 0);
                                ImGui.TextColored(Colors.GetItemRarityColor(glamourItem.Rarity), StringCache.GetItemName(slot->GlamourId));
                                ImGui.SameLine(0, 0);
                                ImGui.Text(")");
                            }
                            if (slot->Stain != 0)
                            {
                                ImGui.Text($"Stain: {slot->Stain}");
                                ImGuiUtils.SameLineSpace();
                                ImGui.Text("(");
                                ImGui.SameLine(0, 0);
                                using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.GetStainColor(slot->Stain)))
                                    ImGui.Bullet();
                                ImGui.SameLine(0, 0);
                                ImGui.Text(StringCache.GetSheetText<Stain>(slot->Stain, "Name"));
                                ImGui.SameLine(0, 0);
                                ImGui.Text(")");
                            }
                        }

                        var usedInGearsets = GetItemInGearsetsList(slot->ItemID, slotIndex);
                        if (usedInGearsets.Count > 1)
                        {
                            ImGuiUtils.DrawPaddedSeparator();
                            ImGui.Text(Service.ClientState.ClientLanguage switch
                            {
                                ClientLanguage.German => "Wird auch in diesen Ausrüstungssets benutzt:",
                                ClientLanguage.French => "Aussi utilisé dans ces ensembles d'équipement :",
                                ClientLanguage.Japanese => "これらのギアセットでも使用されます：",
                                _ => "Also used in these Gearsets:"
                            });
                            using (ImRaii.PushIndent(ImGui.GetStyle().ItemSpacing.X))
                            {
                                foreach (var entry in usedInGearsets)
                                {
                                    if (entry.Id == gearset->ID)
                                        continue;

                                    ImGui.Text($"[{entry.Id + 1}] {entry.Name}");
                                }
                            }
                        }
                    }
                }

                var itemLevelText = $"{item.LevelItem.Row}";
                ImGuiUtils.PushCursorX(IconSize.X * ImGuiHelpers.GlobalScale / 2f - ImGui.CalcTextSize(itemLevelText).X / 2f);
                ImGui.TextColored(GetItemLevelColor(gearset->ClassJob, item, Colors.Red, Colors.Yellow, Colors.Green), itemLevelText);

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
                list.Add(new(gearset->ID, MemoryHelper.ReadStringNullTerminated((nint)gearset->Name)));
        }

        return list;
    }

    private short GetLevelForJob(byte classJob)
    {
        var jobIndex = Service.Data.GetExcelSheet<ClassJob>()?.GetRow(classJob)?.DohDolJobIndex;
        return jobIndex == null ? (short)0 : PlayerState.Instance()->ClassJobLevelArray[(short)jobIndex];
    }

    private Vector4 GetItemLevelColor(byte classJob, Item item, params Vector4[] colors)
    {
        if (colors.Length < 2)
            throw new ArgumentException("At least two colors are required for interpolation.");

        var level = GetLevelForJob(classJob);

        if (!_tweak.MaxLevelRanges.TryGetValue(level, out var range))
            return Colors.White;

        var itemLevel = item.LevelItem.Row;

        // special case for Fisher's Secondary Tool
        // which has only one item, Spearfishing Gig
        if (item.ItemUICategory.Row == 99)
            return itemLevel == 180 ? Colors.Green : Colors.Red;

        if (itemLevel < range.Min)
            return Colors.Red;

        var value = (itemLevel - range.Min) / (float)(range.Max - range.Min);

        var startIndex = (int)(value * (colors.Length - 1));
        var endIndex = Math.Min(startIndex + 1, colors.Length - 1);
        var t = value * (colors.Length - 1) - startIndex;
        return Vector4.Lerp(colors[startIndex], colors[endIndex], t);
    }

    public void DrawItemIcon(GearsetItem* slot, Item item, string key)
    {
        var popupKey = $"##ItemContextMenu_{key}_{item.RowId}_Tooltip";

        //var isEventItem = slot.ItemID > 2000000;
        //var isCollectable = slot.ItemID is > 500000 and < 1000000;
        var isHq = slot->ItemID is > 1000000 and < 1500000;

        var startPos = ImGui.GetCursorPos();

        // icon background
        ImGui.SetCursorPos(startPos);
        Service.TextureCache
            .GetPart("Character", 7, 4)
            .Draw(IconSize * ImGuiHelpers.GlobalScale);

        // icon
        ImGui.SetCursorPos(startPos + IconInset * ImGuiHelpers.GlobalScale);
        Service.TextureCache
            .GetIcon(item.Icon, isHq)
            .Draw((IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

        // icon overlay
        ImGui.SetCursorPos(startPos);
        Service.TextureCache
            .GetPart("Character", 7, 0)
            .Draw(IconSize * ImGuiHelpers.GlobalScale);

        // icon hover effect
        if (ImGui.IsItemHovered() || ImGui.IsPopupOpen(popupKey))
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetCursorPos(startPos);
            Service.TextureCache
                .GetPart("Character", 7, 5)
                .Draw(IconSize * ImGuiHelpers.GlobalScale);
        }

        ImGui.SetCursorPos(startPos + new Vector2(0, (IconSize.Y - 3) * ImGuiHelpers.GlobalScale));

        new ImGuiUtils.ContextMenu(popupKey)
        {
            new ImGuiUtils.ContextMenuEntry()
            {
                Hidden = !item.CanTryOn,
                Label = StringCache.GetAddonText(2426), // "Try On"
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                        AgentTryon.TryOn(0, item.RowId, slot->Stain, 0, 0);
                    else
                        AgentTryon.TryOn(0, item.RowId, slot->Stain, slot->GlamourId, slot->Stain);
                }
            },

            new ImGuiUtils.ContextMenuEntry()
            {
                Label = StringCache.GetAddonText(4379), // "Search for Item"
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    ItemFinderModule.Instance()->SearchForItem(item.RowId);
                }
            },

            new ImGuiUtils.ContextMenuEntry()
            {
                Label = StringCache.GetAddonText(159), // "Copy Item Name"
                ClickCallback = () =>
                {
                    ImGui.SetClipboardText(item.Name);
                }
            },

            new ImGuiUtils.ContextMenuEntry()
            {
                Label = "Open on GarlandTools",
                ClickCallback = () =>
                {
                    Task.Run(() => Util.OpenLink($"https://www.garlandtools.org/db/#item/{item.RowId}"));
                },
                HoverCallback = () =>
                {
                    using var tooltip = ImRaii.Tooltip();

                    var pos = ImGui.GetCursorPos();
                    ImGui.GetWindowDrawList().AddText(
                        UiBuilder.IconFont, 12 * ImGuiHelpers.GlobalScale,
                        ImGui.GetWindowPos() + pos + new Vector2(2),
                        Colors.Grey,
                        FontAwesomeIcon.ExternalLinkAlt.ToIconString()
                    );
                    ImGui.SetCursorPos(pos + new Vector2(20, 0) * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(Colors.Grey, $"https://www.garlandtools.org/db/#item/{item.RowId}");
                }
            },

            new ImGuiUtils.ContextMenuEntry()
            {
                Hidden = !ItemSearchUtils.CanSearchForItem(item.RowId),
                Label = Service.ClientState.ClientLanguage switch
                {
                    ClientLanguage.German => "Auf den M\u00e4rkten suchen",
                    ClientLanguage.French => "Rechercher sur les marchés",
                    ClientLanguage.Japanese => "市場で検索する",
                    _ => "Search the markets"
                },
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    ItemSearchUtils.Search(item.RowId);
                }
            }
        }
        .Draw();
    }
}
