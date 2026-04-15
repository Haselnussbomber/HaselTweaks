using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GearsetEntry = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetEntry;
using GearsetFlag = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetFlag;
using GearsetItem = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetItem;
using GearsetItemIndex = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.GearsetItemIndex;

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
    private readonly ItemService _itemService;
    private readonly PluginConfig _pluginConfig;
    private readonly ISeStringEvaluator _seStringEvaluator;
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
        var slotIndices = Enum.GetValues<GearsetItemIndex>();

        using var padding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, Vector2.Zero)
                                  .Push(ImGuiStyleVar.FramePadding, Vector2.Zero);

        using var table = ImRaii.Table("##Table", 1 + slotIndices.Length, ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        if (_resetScrollPosition)
        {
            ImGui.SetScrollY(0);
            _resetScrollPosition = false;
        }

        ImGui.TableSetupColumn("##ID", ImGuiTableColumnFlags.WidthFixed, (24 + 14 + ImStyle.ItemInnerSpacing.X * 2f + ImStyle.ItemSpacing.X * 2f) * ImStyle.Scale);

        foreach (var slot in slotIndices)
        {
            // skip obsolete belt slot
            if (slot == GearsetItemIndex.Belt)
                continue;

            ImGui.TableSetupColumn($"##Slot{(int)slot}", ImGuiTableColumnFlags.WidthFixed, IconSize.X * ImStyle.Scale);
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
                    ImGui.Dummy(new Vector2(1, IconSize.Y * ImStyle.Scale / 2f));
                }
                continue;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            {
                var startPos = ImCursor.Position;
                var region = ImStyle.ContentRegionAvail;
                var rowHeight = IconSize.Y * ImStyle.Scale + ImStyle.FrameHeight;

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

                    ImGui.Text(_seStringEvaluator.EvaluateFromAddon(4350, [gearset->ItemLevel]).ToString());
                }

                ImGuiContextMenu.Draw("GearsetContext", builder =>
                {
                    builder
                        .AddGearsetLinkGlamour(gearset)
                        .AddGearsetChangeGlamour(gearset)
                        .AddGearsetUnlinkGlamour(gearset)
                        .AddGearsetChangePortrait(gearset);
                });

                var iconSize = 28 * ImStyle.Scale;
                var itemStartPos = startPos + new Vector2(region.X / 2f - iconSize / 2f, ImStyle.ItemInnerSpacing.Y); // start from the right

                // class icon
                ImCursor.Position = itemStartPos;
                _textureProvider.DrawIcon(62100 + gearset->ClassJob, iconSize);

                // gearset number
                var text = $"{gearsetIndex + 1}";
                ImCursor.Position = itemStartPos + new Vector2(iconSize / 2f - ImGui.CalcTextSize(text).X / 2f, iconSize);
                ImGui.Text(text);
            }

            foreach (var slotIndex in slotIndices)
            {
                // skip obsolete belt slot
                if (slotIndex == GearsetItemIndex.Belt)
                    continue;

                using var slotId = ImRaii.PushId($"Slot{slotIndex}");

                var slotItem = gearset->GetItem(slotIndex);

                ImGui.TableNextColumn();

                var itemId = ItemUtil.GetBaseId(slotItem.ItemId).ItemId;
                if (itemId == 0)
                {
                    var cursorPos = ImCursor.Position;

                    // icon background
                    ImCursor.Position = cursorPos;
                    _uldService.DrawPart("Character", 8, 0, IconSize * ImStyle.Scale);

                    ImCursor.Position = cursorPos + IconInset * ImStyle.Scale;
                    var iconIndex = slotIndex switch
                    {
                        GearsetItemIndex.RingLeft => GearsetItemIndex.RingRight, // left ring
                        _ => slotIndex,
                    };
                    _uldService.DrawPart("Character", 12, 17 + (uint)iconIndex, (IconSize - IconInset * 2f) * ImStyle.Scale);

                    continue;
                }

                if (!_excelService.TryGetRow<Item>(itemId, out var item))
                    continue;

                ImCursor.Y += 2f * ImStyle.Scale;

                DrawItemIcon(gearset, slotIndex, slotItem, item);

                if (slotIndex == GearsetItemIndex.SoulStone)
                    continue;

                var itemLevelText = $"{item.LevelItem.RowId}";
                ImCursor.X += IconSize.X * ImStyle.Scale / 2f - ImGui.CalcTextSize(itemLevelText).X / 2f;
                ImGui.TextColored(_itemService.GetItemLevelColor(item, gearset->ClassJob, Color.Red, Color.Yellow, Color.Green), itemLevelText);

                ImCursor.Y += 2f * ImStyle.Scale;
            }
        }
    }

    private record ItemInGearsetEntry(byte Id, string Name);
    private List<ItemInGearsetEntry> GetItemInGearsetsList(uint itemId, GearsetItemIndex slotIndex)
    {
        var list = new List<ItemInGearsetEntry>();

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        var permittedGearsetCount = InventoryManager.Instance()->GetPermittedGearsetCount();
        for (var gearsetIndex = 0; gearsetIndex < permittedGearsetCount; gearsetIndex++)
        {
            var gearset = raptureGearsetModule->GetGearset(gearsetIndex);
            if (!gearset->Flags.HasFlag(GearsetFlag.Exists))
                continue;

            ref var item = ref gearset->GetItem(slotIndex);
            if (item.ItemId == itemId)
                list.Add(new(gearset->Id, gearset->NameString));
        }

        return list;
    }

    public void DrawItemIcon(GearsetEntry* gearset, GearsetItemIndex slotIndex, in GearsetItem slotItem, ItemHandle item)
    {
        var startPos = ImCursor.Position;

        // icon background
        _uldService.DrawPart("Character", 7, 4, IconSize * ImStyle.Scale);

        // icon
        ImCursor.Position = startPos + IconInset * ImStyle.Scale;
        _textureProvider.DrawIcon(new GameIconLookup(_itemService.GetItemIcon(item), ItemUtil.IsHighQuality(slotItem.ItemId)), (IconSize - IconInset * 2f) * ImStyle.Scale);

        // icon overlay
        ImCursor.Position = startPos;
        _uldService.DrawPart("Character", 7, 0, IconSize * ImStyle.Scale);

        // icon hover effect
        if (ImGui.IsItemHovered() || ImGui.IsPopupOpen("ItemTooltip"))
        {
            ImCursor.Position = startPos;
            _uldService.DrawPart("Character", 7, 5, IconSize * ImStyle.Scale);
        }

        ImCursor.Position = startPos + new Vector2(0, (IconSize.Y - 3) * ImStyle.Scale);

        var (glamourId, stain0Id, stain1Id) = (slotItem.GlamourId, slotItem.Stain0Id, slotItem.Stain1Id);
        ImGuiContextMenu.Draw("ItemTooltip", builder =>
        {
            builder
                .AddTryOn(item, glamourId, stain0Id, stain1Id)
                .AddItemFinder(item)
                .AddCopyItemName(item)
                .AddOpenOnGarlandTools("item", item.ItemId)
                .AddItemSearch(item);
        });

        if (!ImGui.IsItemHovered() || !_itemService.TryGetItem(item, out var itemRow))
            return;

        using var _ = ImRaii.Tooltip();

        ImGui.TextColored(_itemService.GetItemRarityColor(item), _itemService.GetItemName(item).ToString());

        var holdingShift = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
        if (holdingShift)
        {
            ImCursor.SameLineSpace();
            ImGui.Text($"[{item.ItemId}]");
        }

        if (itemRow.ItemUICategory.IsValid)
        {
            ImCursor.Y += -ImStyle.ItemSpacing.Y;
            ImGui.Text(itemRow.ItemUICategory.Value.Name.ToString() ?? string.Empty);
        }

        var glamourItem = new ItemHandle(slotItem.GlamourId);

        if (!glamourItem.IsEmpty || slotItem.Stain0Id != 0 || slotItem.Stain1Id != 0)
            ImGuiUtils.DrawPaddedSeparator();

        if (!glamourItem.IsEmpty)
        {
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.LabelGlamour"));
            ImCursor.SameLineSpace();
            ImGui.TextColored(_itemService.GetItemRarityColor(glamourItem), _itemService.GetItemName(glamourItem).ToString());

            if (holdingShift)
            {
                ImCursor.SameLineSpace();
                ImGui.Text($"[{slotItem.GlamourId}]");
            }
        }

        if (slotItem.Stain0Id != 0 && _excelService.TryGetRow<Stain>(slotItem.Stain0Id, out var stain0))
        {
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.LabelDye0"));
            ImCursor.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, stain0.GetColor().ToUInt()))
                ImGui.Bullet();
            ImGui.SameLine(0, 0);
            ImGui.Text(_textService.GetStainName(slotItem.Stain0Id));

            if (holdingShift)
            {
                ImCursor.SameLineSpace();
                ImGui.Text($"[{slotItem.Stain0Id}]");
            }
        }

        if (slotItem.Stain1Id != 0 && _excelService.TryGetRow<Stain>(slotItem.Stain1Id, out var stain1))
        {
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.LabelDye1"));
            ImCursor.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, stain1.GetColor().ToUInt()))
                ImGui.Bullet();
            ImGui.SameLine(0, 0);
            ImGui.Text(_textService.GetStainName(slotItem.Stain1Id));

            if (holdingShift)
            {
                ImCursor.SameLineSpace();
                ImGui.Text($"[{slotItem.Stain1Id}]");
            }
        }

        var usedInGearsets = GetItemInGearsetsList(slotItem.ItemId, slotIndex);
        if (usedInGearsets.Count > 1)
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGui.Text(_textService.Translate("GearSetGridWindow.ItemTooltip.AlsoUsedInTheseGearsets"));
            using (ImRaii.PushIndent(ImStyle.ItemSpacing.X))
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
