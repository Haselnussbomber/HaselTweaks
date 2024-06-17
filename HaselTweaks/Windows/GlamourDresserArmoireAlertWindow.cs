using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselCommon;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselCommon.Utils;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows;

public unsafe class GlamourDresserArmoireAlertWindow : SimpleWindow
{
    private const int NumPrismBoxSlots = 800;
    private static readonly Vector2 IconSize = new(34);
    private readonly TextureManager TextureManager;

    private static AddonMiragePrismPrismBox* Addon => GetAddon<AddonMiragePrismPrismBox>("MiragePrismPrismBox");

    public GlamourDresserArmoireAlertWindow(
        WindowManager windowManager,
        TextureManager textureManager)
        : base(windowManager, t("GlamourDresserArmoireAlertWindow.Title"))
    {
        TextureManager = textureManager;

        DisableWindowSounds = true;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoResize;
        Flags |= ImGuiWindowFlags.NoMove;

        SizeCondition = ImGuiCond.Always;
        Size = new(360, 428);
    }

    public GlamourDresserArmoireAlert? Tweak { get; set; }

    public override bool DrawConditions()
        => Tweak != null && Addon != null && Addon->AtkUnitBase.IsVisible && Tweak!.Categories.Count != 0;

    public override void Draw()
    {
        ImGui.TextWrapped(t("GlamourDresserArmoireAlertWindow.Info"));

        foreach (var (categoryId, categoryItems) in Tweak!.Categories.OrderBy(kv => kv.Key))
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
            TextureManager
                .GetIcon(item.Icon, isHq)
                .Draw(IconSize * ImGuiHelpers.GlobalScale);

            ImGui.SameLine();

            var pos = ImGui.GetCursorPos();
            if (ImGui.Selectable(
                "##Selectable",
                false,
                Tweak!.UpdatePending
                    ? ImGuiSelectableFlags.Disabled
                    : ImGuiSelectableFlags.None,
                ImGuiHelpers.ScaledVector2(ImGui.GetContentRegionAvail().X, IconSize.Y)))
            {
                RestoreItem(itemIndex);
            }

            ImGui.SetCursorPos(pos + new Vector2(
                ImGui.GetStyle().ItemInnerSpacing.X,
                IconSize.Y * ImGuiHelpers.GlobalScale / 2f - ImGui.GetTextLineHeight() / 2f - 1));

            ImGui.TextUnformatted(GetItemName(item.RowId));
        }

        ImGuiContextMenu.Draw(popupKey, [
            ImGuiContextMenu.CreateTryOn(item),
            ImGuiContextMenu.CreateItemFinder(item.RowId),
            ImGuiContextMenu.CreateCopyItemName(item.RowId),
            ImGuiContextMenu.CreateOpenOnGarlandTools("item", item.RowId),
            ImGuiContextMenu.CreateItemSearch(item)
        ]);
    }

    private void RestoreItem(uint itemIndex)
    {
        Tweak!.UpdatePending = MirageManager.Instance()->RestorePrismBoxItem(itemIndex);
    }
}
