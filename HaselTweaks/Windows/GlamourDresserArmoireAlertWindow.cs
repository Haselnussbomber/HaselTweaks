using System.Collections.Generic;
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
    private static AddonMiragePrismPrismBox* Addon => GetAddon<AddonMiragePrismPrismBox>("MiragePrismPrismBox");

    private readonly Dictionary<uint, HashSet<(uint, ExtendedItem, bool)>> Categories = new();
    private uint[]? LastItemIds = null;
    private bool UpdatePending = false;

    public GlamourDresserArmoireAlertWindow() : base(t("GlamourDresserArmoireAlertWindow.Title"))
    {
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
        => Addon != null && Addon->AtkUnitBase.IsVisible && Categories.Any();

    public override void Update()
    {
        var mirageManager = MirageManager.Instance();

        var itemIds = new Span<uint>(mirageManager->PrismBoxItemIds, NumPrismBoxSlots);

        if (LastItemIds != null && itemIds.SequenceEqual(LastItemIds))
            return;

        LastItemIds = itemIds.ToArray();

        Categories.Clear();

        Service.PluginLog.Info($"[{nameof(GlamourDresserArmoireAlert)}] Updating...");

        for (var i = 0u; i < NumPrismBoxSlots; i++)
        {
            var itemId = mirageManager->PrismBoxItemIds[i];
            if (itemId == 0)
                continue;

            var isHq = itemId is > 1000000 and < 1500000;
            itemId %= 1000000;

            var item = GetRow<ExtendedItem>(itemId);
            if (item == null)
                continue;

            var cabinet = FindRow<Cabinet>(row => row.Item.Row == itemId);
            if (cabinet == null)
                continue;

            if (!Categories.TryGetValue(item.ItemUICategory.Row, out var categorySet))
            {
                Categories.TryAdd(item.ItemUICategory.Row, categorySet = new());
            }

            var key = (i, item, isHq);
            if (!categorySet.Contains(key))
            {
                categorySet.Add(key);
            }
        }

        UpdatePending = false;
    }

    public override void Draw()
    {
        ImGui.TextWrapped(t("GlamourDresserArmoireAlertWindow.Info"));

        foreach (var (categoryId, categoryDict) in Categories.OrderBy(kv => kv.Key))
        {
            var category = GetRow<ItemUICategory>(categoryId)!;

            ImGui.TextUnformatted(category.Name.ToDalamudString().ToString());
            ImGuiUtils.PushCursorY(3 * ImGuiHelpers.GlobalScale);

            using var indent = ImRaii.PushIndent();

            foreach (var (itemIndex, item, isHq) in categoryDict)
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
                UpdatePending
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
            ImGuiContextMenu.CreateOpenOnGarlandTools(item.RowId),
            ImGuiContextMenu.CreateItemSearch(item),
        }
        .Draw();
    }

    private void RestoreItem(uint itemIndex)
    {
        UpdatePending = MirageManager.Instance()->RestorePrismBoxItem(itemIndex);
    }
}
