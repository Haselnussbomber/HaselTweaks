using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class GlamourDresserAlertWindow : SimpleWindow
{
    private static readonly Vector2 IconSize = new(34);

    private readonly ILogger<GlamourDresserAlertWindow> _logger;
    private readonly ITextureProvider _textureProvider;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ItemService _itemService;
    private readonly GlamourDresserAlert _tweak;
    private readonly WindowManager _windowManager;
    private readonly CabinetService _cabinetService;

    public bool IsUpdatePending { get; set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        DisableWindowSounds = true;
        RespectCloseHotkey = false;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoMove;

        SizeCondition = ImGuiCond.Appearing;
        Size = new(370, 428);

        TitleBarButtons.Add(new()
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new(0, 1),
            ShowTooltip = () =>
            {
                var isWindowOpen = _windowManager.TryGetWindow<PluginWindow>(out var pluginWindow) && pluginWindow.IsOpen;
                ImGui.SetTooltip(_textService.Translate(isWindowOpen ? "TitleBarButton.CloseConfig" : "TitleBarButton.OpenConfig"));
            },
            Click = (button) => _windowManager.CreateOrToggle<PluginWindow>().SelectTweak<GlamourDresserAlert>()
        });
    }

    public override bool DrawConditions()
    {
        return IsAddonOpen("MiragePrismPrismBox"u8)
            && !IsAddonOpen("MiragePrismMiragePlate"u8)
            && _tweak.HasAnyItems;
    }

    public override void PreDraw()
    {
        if (!TryGetAddon<AtkUnitBase>("MiragePrismPrismBox"u8, out var addon))
            return;

        var width = addon->GetScaledWidth(true);
        var offset = new Vector2(width - 12, 9);

        Position = ImGui.GetMainViewport().Pos + addon->Position + offset;
    }

    public override void Draw()
    {
        ImGui.TextWrapped(_textService.Translate("GlamourDresserAlertWindow.Info"));

        DrawItems(nameof(_tweak.CabinetStorableItems), _tweak.CabinetStorableItems);
        DrawItems(nameof(_tweak.OutfitConvertibleItems), _tweak.OutfitConvertibleItems);
        DrawItems(nameof(_tweak.DuplicateItemsInExistingSets), _tweak.DuplicateItemsInExistingSets);
        DrawItems(nameof(_tweak.DuplicateItems), _tweak.DuplicateItems);
    }

    public void DrawItems(string key, Dictionary<uint, HashSet<ItemHandle>> items)
    {
        if (items.Count == 0)
            return;

        var itemCount = items.Sum(kv => kv.Value.Count);
        var label = _textService.EvaluateTranslatedSeString($"GlamourDresserAlertWindow.{key}TreeNodeLabel", itemCount).ToString();
        using var treeNode = ImRaii.TreeNode($"{label}###{key}TreeNode", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treeNode)
            return;

        foreach (var (categoryId, categoryItems) in items.OrderBy(kv => kv.Key))
        {
            if (!_excelService.TryGetRow<ItemUICategory>(categoryId, out var category))
                continue;

            ImGui.Text(category.Name.ToString());
            ImCursor.Y += 3 * ImStyle.Scale;

            using var indent = ImRaii.PushIndent();

            foreach (var item in categoryItems)
            {
                DrawItem(item, true);
            }
        }
    }

    public void DrawItem(ItemHandle item, bool isCabinetCategory)
    {
        using var id = ImRaii.PushId($"Item{item.ItemId}");

        using (var group = ImRaii.Group())
        {
            _textureProvider.DrawIcon(new GameIconLookup(_itemService.GetItemIcon(item), item.IsHighQuality), IconSize * ImStyle.Scale);

            ImGui.SameLine();

            var pos = ImCursor.Position;

            if (ImGui.Selectable(
                "##Selectable",
                false,
                IsUpdatePending
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None,
                ImGuiHelpers.ScaledVector2(ImStyle.ContentRegionAvail.X, IconSize.Y)))
            {
                RestoreItem(item);
            }

            if (ImGui.IsItemHovered())
            {
                // make sure items are loaded, otherwise restoring will fail

                if (_excelService.TryGetRow<MirageStoreSetItem>(item, out var set))
                {
                    HaselExdModule.GetMirageStoreSetItemRowById(item);

                    foreach (var setItem in set.Items.Where(item => item.RowId != 0))
                        ExdModule.GetItemRowById(setItem.RowId);
                }
                else
                {
                    ExdModule.GetItemRowById(item);
                }

                if (isCabinetCategory && _excelService.TryGetRow<MirageStoreSetItem>(item, out var lookupRow) && lookupRow.Items.Any(setItem => _cabinetService.IsItemCollected(setItem)))
                {
                    using var tooltip = ImRaii.Tooltip();

                    ImGui.Text(_textService.Translate("GlamourDresserAlertWindow.DuplicateSetItemsTooltip"));

                    foreach (var setItem in lookupRow.Items)
                    {
                        if (setItem.RowId == 0 || !setItem.IsValid)
                            continue;

                        if (!_cabinetService.IsItemCollected(setItem))
                            continue;

                        ImGuiP.RenderBullet(ImGui.GetWindowDrawList(), ImCursor.ScreenPosition + new Vector2(ImStyle.FramePadding.X + ImGui.GetFontSize() * 0.5f, ImGui.GetFontSize() * 0.5f), Color.White.ToUInt());
                        ImCursor.X += ImGui.GetFontSize() + ImStyle.FramePadding.X * 2;
                        using (_itemService.GetItemRarityColor(setItem.RowId).Push(ImGuiCol.Text))
                            ImGui.Text(_textService.GetItemName(setItem.RowId).ToString());
                    }
                }
            }

            ImCursor.Position = pos + new Vector2(
                ImStyle.ItemInnerSpacing.X,
                IconSize.Y * ImStyle.Scale / 2f - ImStyle.TextLineHeight / 2f - 1);

            ImGui.Text(_itemService.GetItemName(item, false).ToString());
        }

        ImGuiContextMenu.Draw("ItemContextMenu", builder =>
        {
            builder
                .AddTryOn(item)
                .AddItemFinder(item)
                .AddCopyItemName(item)
                .AddOpenOnGarlandTools("item", item)
                .AddItemSearch(item);
        });
    }

    private void RestoreItem(ItemHandle item)
    {
        var mirageManager = MirageManager.Instance();
        if (!mirageManager->PrismBoxRequested)
            return;
        if (!mirageManager->PrismBoxLoaded)
            return;

        var itemId = item.BaseItemId;
        var itemIndex = mirageManager->PrismBoxItemIds.IndexOf(itemId);
        if (itemIndex == -1)
            return;

        if (_excelService.TryGetRow<MirageStoreSetItem>(itemId, out var lookupRow))
        {
            // stain ids store bits of which slots are locked.
            // we invert that to know which slots are stored in the glamour dresser.
            // then we mask the value by which slots/bits actually have items in the sheet

            var bits = stackalloc byte[2];
            bits[0] = (byte)~mirageManager->PrismBoxStain0Ids[itemIndex];
            bits[1] = (byte)~mirageManager->PrismBoxStain1Ids[itemIndex];

            ushort mask = 0;

            for (var i = 0; i < lookupRow.Items.Count; i++)
            {
                if (lookupRow.Items[i].RowId != 0)
                {
                    mask |= (ushort)(1 << i);
                }
            }

            *(ushort*)bits &= mask;

            _logger.LogDebug("Restoring set items {index} with bits {b1} | {b2}", itemIndex, Convert.ToString(bits[0], 2), Convert.ToString(bits[1], 2));
            IsUpdatePending = mirageManager->RestorePrismBoxSetItem((uint)itemIndex, bits);
        }
        else
        {
            _logger.LogDebug("Restoring item {index}", itemIndex);
            IsUpdatePending = mirageManager->RestorePrismBoxItem((uint)itemIndex);
        }
    }
}
