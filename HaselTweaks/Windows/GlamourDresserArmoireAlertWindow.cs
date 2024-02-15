using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Sheets;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows;

public unsafe class GlamourDresserArmoireAlertWindow : Window
{
    private const int NumPrismBoxSlots = 800;
    private static readonly Vector2 IconSize = new(34);

    private readonly GlamourDresserArmoireAlert _tweak;

    private static AddonMiragePrismPrismBox* Addon => GetAddon<AddonMiragePrismPrismBox>("MiragePrismPrismBox");

    public GlamourDresserArmoireAlertWindow(GlamourDresserArmoireAlert tweak) : base(t("GlamourDresserArmoireAlertWindow.Title"))
    {
        _tweak = tweak;

        IsOpen = true;
        DisableWindowSounds = true;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoResize;
        Flags |= ImGuiWindowFlags.NoMove;

        SizeCondition = ImGuiCond.Always;
        Size = new(360, 428);
    }

    public override void OnClose()
    {
        Service.WindowManager.CloseWindow<GearSetGridWindow>();
    }

    public override bool DrawConditions()
        => Addon != null && Addon->AtkUnitBase.IsVisible && _tweak.Categories.Any();

    public override void Draw()
    {
        ImGui.TextWrapped(t("GlamourDresserArmoireAlertWindow.Info"));

        foreach (var (categoryId, categoryItems) in _tweak.Categories.OrderBy(kv => kv.Key))
        {
            var category = GetRow<ItemUICategory>(categoryId)!;

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

    public void DrawItem(uint itemIndex, ExtendedItem item, bool isHq)
    {
        var popupKey = $"##ItemContextMenu_{item.RowId}_Tooltip";

        using (var group = ImRaii.Group())
        {
            Service.TextureManager
                .GetIcon(item.Icon, isHq)
                .Draw(IconSize * ImGuiHelpers.GlobalScale);

            ImGui.SameLine();

            var pos = ImGui.GetCursorPos();
            if (ImGui.Selectable(
                "##Selectable",
                false,
                _tweak.UpdatePending
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None,
                ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, IconSize.Y)))
            {
                RestoreItem(itemIndex);
            }

            ImGui.SetCursorPos(pos + new Vector2(
                ImGui.GetStyle().ItemInnerSpacing.X,
                IconSize.Y * ImGuiHelpers.GlobalScale / 2f - ImGui.GetTextLineHeight() / 2f - 1));

            ImGui.Text(GetItemName(item.RowId));
        }

        new ImGuiContextMenu(popupKey)
        {
            ImGuiContextMenu.CreateTryOn(item),
            ImGuiContextMenu.CreateItemFinder(item.RowId),
            ImGuiContextMenu.CreateCopyItemName(item.RowId),
            ImGuiContextMenu.CreateOpenOnGarlandTools("item", item.RowId),
            ImGuiContextMenu.CreateItemSearch(item),
        }
        .Draw();
    }

    private void RestoreItem(uint itemIndex)
    {
        _tweak.UpdatePending = MirageManager.Instance()->RestorePrismBoxItem(itemIndex);
    }
}
