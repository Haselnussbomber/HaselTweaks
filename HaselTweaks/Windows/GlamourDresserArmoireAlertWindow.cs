using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class GlamourDresserArmoireAlertWindow : SimpleWindow
{
    private static readonly Vector2 IconSize = new(34);

    private readonly TextureService _textureService;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ImGuiContextMenuService _imGuiContextMenuService;
    private readonly GlamourDresserArmoireAlert? _tweak;

    private static AddonMiragePrismPrismBox* Addon => GetAddon<AddonMiragePrismPrismBox>("MiragePrismPrismBox");

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
        => _tweak != null && Addon != null && Addon->AtkUnitBase.IsVisible && _tweak!.Categories.Count != 0;

    public override void Draw()
    {
        ImGuiHelpers.SafeTextWrapped(_textService.Translate("GlamourDresserArmoireAlertWindow.Info"));

        foreach (var (categoryId, categoryItems) in _tweak!.Categories.OrderBy(kv => kv.Key))
        {
            if (!_excelService.TryGetRow<ItemUICategory>(categoryId, out var category))
                continue;

            ImGui.TextUnformatted(category.Name.ToDalamudString().ToString());
            ImGuiUtils.PushCursorY(3 * ImGuiHelpers.GlobalScale);

            using var indent = ImRaii.PushIndent();

            foreach (var (itemIndex, (item, isHq)) in categoryItems)
            {
                DrawItem(itemIndex, item, isHq);
            }
        }

        Position = new(
            Addon->AtkUnitBase.X + Addon->AtkUnitBase.GetScaledWidth(true) - 12,
            Addon->AtkUnitBase.Y + 9
        );
    }

    public void DrawItem(uint itemIndex, Item item, bool isHq)
    {
        var popupKey = $"##ItemContextMenu_{item.RowId}_Tooltip";

        using (var group = ImRaii.Group())
        {
            _textureService.DrawIcon(new GameIconLookup(item.Icon, isHq), IconSize * ImGuiHelpers.GlobalScale);

            ImGui.SameLine();

            var pos = ImGui.GetCursorPos();
            if (ImGui.Selectable(
                "##Selectable",
                false,
                _tweak!.UpdatePending
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None,
                ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, IconSize.Y)))
            {
                RestoreItem(itemIndex);
            }

            ImGui.SetCursorPos(pos + new Vector2(
                ImGui.GetStyle().ItemInnerSpacing.X,
                IconSize.Y * ImGuiHelpers.GlobalScale / 2f - ImGui.GetTextLineHeight() / 2f - 1));

            ImGui.TextUnformatted(_textService.GetItemName(item.RowId));
        }

        _imGuiContextMenuService.Draw(popupKey, builder =>
        {
            builder
                .AddTryOn(item)
                .AddItemFinder(item.RowId)
                .AddCopyItemName(item.RowId)
                .AddOpenOnGarlandTools("item", item.RowId)
                .AddItemSearch(item);
        });
    }

    private void RestoreItem(uint itemIndex)
    {
        _tweak!.UpdatePending = MirageManager.Instance()->RestorePrismBoxItem(itemIndex);
    }
}
