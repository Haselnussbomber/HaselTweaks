using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class GlamourDresserArmoireAlertWindow : SimpleWindow
{
    private static readonly Vector2 IconSize = new(34);

    private readonly ILogger<GlamourDresserArmoireAlertWindow> _logger;
    private readonly ITextureProvider _textureProvider;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ItemService _itemService;
    private readonly GlamourDresserArmoireAlert _tweak;

    public bool IsUpdatePending { get; set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        DisableWindowSounds = true;
        RespectCloseHotkey = false;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoResize;
        Flags |= ImGuiWindowFlags.NoMove;

        SizeCondition = ImGuiCond.Always;
        Size = new(360, 428);
    }

    public override bool DrawConditions()
        => IsAddonOpen("MiragePrismPrismBox"u8) && _tweak.Categories.Count != 0;

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
        ImGui.TextWrapped(_textService.Translate("GlamourDresserArmoireAlertWindow.Info"));

        foreach (var (categoryId, categoryItems) in _tweak.Categories.OrderBy(kv => kv.Key))
        {
            if (!_excelService.TryGetRow<ItemUICategory>(categoryId, out var category))
                continue;

            ImGui.Text(category.Name.ToString());
            ImGuiUtils.PushCursorY(3 * ImGuiHelpers.GlobalScale);

            using var indent = ImRaii.PushIndent();

            foreach (var item in categoryItems)
            {
                DrawItem(item);
            }
        }
    }

    public void DrawItem(ItemHandle item)
    {
        using var id = ImRaii.PushId($"Item{item.ItemId}");

        using (var group = ImRaii.Group())
        {
            _textureProvider.DrawIcon(new GameIconLookup(_itemService.GetItemIcon(item), item.IsHighQuality), IconSize * ImGuiHelpers.GlobalScale);

            ImGui.SameLine();

            var pos = ImGui.GetCursorPos();
            if (ImGui.Selectable(
                "##Selectable",
                false,
                IsUpdatePending
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None,
                ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, IconSize.Y)))
            {
                RestoreItem(item);
            }

            if (ImGui.IsItemHovered())
            {
                ExdModule.GetItemRowById(item); // make sure item is loaded, otherwise RestorePrismBoxItem will fail
            }

            ImGui.SetCursorPos(pos + new Vector2(
                ImGui.GetStyle().ItemInnerSpacing.X,
                IconSize.Y * ImGuiHelpers.GlobalScale / 2f - ImGui.GetTextLineHeight() / 2f - 1));

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
        if (!mirageManager->PrismBoxLoaded)
            return;

        var itemIndex = mirageManager->PrismBoxItemIds.IndexOf(item);
        if (itemIndex == -1)
            return;

        _logger.LogDebug("Restoring item {index}", itemIndex);
        IsUpdatePending = mirageManager->RestorePrismBoxItem((uint)itemIndex);
    }
}
