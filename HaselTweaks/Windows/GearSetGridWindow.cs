using Dalamud.Interface.Textures;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GearsetEntry = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetEntry;
using GearsetFlag = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetFlag;
using GearsetItem = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetItem;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class GearSetGridWindow : SimpleWindow
{
    private static readonly Vector2 IconSize = new(34);
    private static readonly Vector2 IconInset = IconSize * 0.08333f;
    private static readonly float ItemCellWidth = IconSize.X;
    private readonly IClientState _clientState;
    private readonly ITextureProvider _textureProvider;
    private readonly UldService _uldService;
    private readonly ExcelService _excelService;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly ImGuiContextMenuService _imGuiContextMenuService;
    private readonly ItemService _itemService;
    private readonly PluginConfig _pluginConfig;
    private bool _resetScrollPosition;

    public GearSetGridConfiguration Config => _pluginConfig.Tweaks.GearSetGrid;

    [AutoPostConstruct]
    private void Initialize()
    {
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
        => _clientState.IsLoggedIn;

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
                    UIGlobals.PlaySoundEffect(8);
                    raptureGearsetModule->EquipGearset(gearsetIndex);
                }
                if (ImGui.IsItemHovered())
                {
                    using var tooltip = ImRaii.Tooltip();
                    ImGui.Text($"[{gearset->Id + 1}] {name}");

                    if (gearset->GlamourSetLink != 0)
                    {
                        ImGui.Text($"{_textService.GetAddonText(3185)}: {gearset->GlamourSetLink}"); // "Glamour Plate: {link}"
                    }
                }

                _imGuiContextMenuService.Draw("GearsetContext", builder =>
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
                _textureProvider.DrawIcon(62100 + gearset->ClassJob, iconSize);

                // gearset number
                var text = $"{gearsetIndex + 1}";
                ImGui.SetCursorPos(itemStartPos + new Vector2(iconSize / 2f - ImGui.CalcTextSize(text).X / 2f, iconSize));
                ImGui.Text(text);
            }

            for (var slotIndex = 0u; slotIndex < NUM_SLOTS; slotIndex++)
            {
                using var slotId = ImRaii.PushId($"Slot_{slotIndex}");

                // skip obsolete belt slot
                if (slotIndex == 5)
                    continue;

                var slot = gearset->Items.GetPointer((int)slotIndex);

                ImGui.TableNextColumn();

                var itemId = ItemUtil.GetBaseId(slot->ItemId).ItemId;
                if (itemId == 0)
                {
                    var windowPos = ImGui.GetWindowPos();
                    var cursorPos = ImGui.GetCursorPos();

                    // icon background
                    ImGui.SetCursorPos(cursorPos);
                    _uldService.DrawPart("Character", 8, 0, IconSize * ImGuiHelpers.GlobalScale);

                    ImGui.SetCursorPos(cursorPos + IconInset * ImGuiHelpers.GlobalScale);
                    var iconIndex = slotIndex switch
                    {
                        12 => 11u, // left ring
                        _ => slotIndex,
                    };
                    _uldService.DrawPart("Character", 12, 17 + iconIndex, (IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

                    continue;
                }

                if (!_excelService.TryGetRow<Item>(itemId, out var item))
                    continue;

                ImGuiUtils.PushCursorY(2f * ImGuiHelpers.GlobalScale);

                DrawItemIcon(gearset, slotIndex, slot, item);

                var itemLevelText = $"{item.LevelItem.RowId}";
                ImGuiUtils.PushCursorX(IconSize.X * ImGuiHelpers.GlobalScale / 2f - ImGui.CalcTextSize(itemLevelText).X / 2f);
                ImGui.TextColored(_itemService.GetItemLevelColor(gearset->ClassJob, item, Color.Red, Color.Yellow, Color.Green), itemLevelText);

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

    public void DrawItemIcon(GearsetEntry* gearset, uint slotIndex, GearsetItem* slot, Item item)
    {
        var startPos = ImGui.GetCursorPos();

        // icon background
        ImGui.SetCursorPos(startPos);
        _uldService.DrawPart("Character", 7, 4, IconSize * ImGuiHelpers.GlobalScale);

        // icon
        ImGui.SetCursorPos(startPos + IconInset * ImGuiHelpers.GlobalScale);
        _textureProvider.DrawIcon(new GameIconLookup(item.Icon, ItemUtil.IsHighQuality(slot->ItemId)), (IconSize - IconInset * 2f) * ImGuiHelpers.GlobalScale);

        // icon overlay
        ImGui.SetCursorPos(startPos);
        _uldService.DrawPart("Character", 7, 0, IconSize * ImGuiHelpers.GlobalScale);

        // icon hover effect
        if (ImGui.IsItemHovered() || ImGui.IsPopupOpen("ItemTooltip"))
        {
            ImGui.SetCursorPos(startPos);
            _uldService.DrawPart("Character", 7, 5, IconSize * ImGuiHelpers.GlobalScale);
        }

        ImGui.SetCursorPos(startPos + new Vector2(0, (IconSize.Y - 3) * ImGuiHelpers.GlobalScale));

        _imGuiContextMenuService.Draw("ItemTooltip", builder =>
        {
            builder
                .AddTryOn(item.RowId, slot->GlamourId, slot->Stain0Id, slot->Stain1Id)
                .AddItemFinder(item.RowId)
                .AddCopyItemName(item.RowId)
                .AddOpenOnGarlandTools("item", item.RowId)
                .AddItemSearch(item.RowId);
        });

        if (!ImGui.IsItemHovered())
            return;

        using var _ = ImRaii.Tooltip();

        ImGui.TextColored(_itemService.GetItemRarityColor(item.RowId), _textService.GetItemName(item.RowId).ToString());

        var holdingShift = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
        if (holdingShift)
        {
            ImGuiUtils.SameLineSpace();
            ImGui.Text($"[{item.RowId}]");
        }

        if (item.ItemUICategory.IsValid)
        {
            ImGuiUtils.PushCursorY(-ImGui.GetStyle().ItemSpacing.Y);
            ImGui.Text(item.ItemUICategory.Value.Name.ToString() ?? string.Empty);
        }

        if (slot->GlamourId != 0 || slot->Stain0Id != 0 || slot->Stain1Id != 0)
            ImGuiUtils.DrawPaddedSeparator();

        if (slot->GlamourId != 0 && _excelService.TryGetRow<Item>(slot->GlamourId, out var glamourItem))
        {
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.LabelGlamour"));
            ImGuiUtils.SameLineSpace();
            ImGui.TextColored(_itemService.GetItemRarityColor(glamourItem.RowId), _textService.GetItemName(slot->GlamourId).ToString());

            if (holdingShift)
            {
                ImGuiUtils.SameLineSpace();
                ImGui.Text($"[{slot->GlamourId}]");
            }
        }

        if (slot->Stain0Id != 0 && _excelService.TryGetRow<Stain>(slot->Stain0Id, out var stain0))
        {
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.LabelDye0"));
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, stain0.GetColor().ToUInt()))
                ImGui.Bullet();
            ImGui.SameLine(0, 0);
            ImGui.Text(stain0.Name.ToString().FirstCharToUpper(_languageProvider.CultureInfo));

            if (holdingShift)
            {
                ImGuiUtils.SameLineSpace();
                ImGui.Text($"[{slot->Stain0Id}]");
            }
        }

        if (slot->Stain1Id != 0 && _excelService.TryGetRow<Stain>(slot->Stain1Id, out var stain1))
        {
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.LabelDye1"));
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, stain1.GetColor().ToUInt()))
                ImGui.Bullet();
            ImGui.SameLine(0, 0);
            ImGui.Text(stain1.Name.ToString().FirstCharToUpper(_languageProvider.CultureInfo));

            if (holdingShift)
            {
                ImGuiUtils.SameLineSpace();
                ImGui.Text($"[{slot->Stain1Id}]");
            }
        }

        var usedInGearsets = GetItemInGearsetsList(slot->ItemId, slotIndex);
        if (usedInGearsets.Count > 1)
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.AlsoUsedInTheseGearsets"));
            using (ImRaii.PushIndent(ImGui.GetStyle().ItemSpacing.X))
            {
                foreach (var entry in usedInGearsets)
                {
                    if (entry.Id == gearset->Id)
                        continue;

                    ImGui.Text($"[{entry.Id + 1}] {entry.Name}");
                }
            }
        }
    }
}
