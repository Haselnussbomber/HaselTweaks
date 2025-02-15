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

[RegisterSingleton]
public unsafe class GlamourDresserArmoireAlertWindow : SimpleWindow
{
    private static readonly Vector2 IconSize = new(34);
    private readonly TextureService TextureService;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly ImGuiContextMenuService ImGuiContextMenuService;

    private static AddonMiragePrismPrismBox* Addon => GetAddon<AddonMiragePrismPrismBox>("MiragePrismPrismBox");

    public GlamourDresserArmoireAlertWindow(
        WindowManager windowManager,
        LanguageProvider languageProvider,
        TextService textService,
        TextureService textureService,
        ExcelService excelService,
        ImGuiContextMenuService imGuiContextMenuService)
        : base(windowManager, textService, languageProvider)
    {
        TextureService = textureService;
        ExcelService = excelService;
        TextService = textService;
        ImGuiContextMenuService = imGuiContextMenuService;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;

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
        ImGuiHelpers.SafeTextWrapped(TextService.Translate("GlamourDresserArmoireAlertWindow.Info"));

        foreach (var (categoryId, categoryItems) in Tweak!.Categories.OrderBy(kv => kv.Key))
        {
            if (!ExcelService.TryGetRow<ItemUICategory>(categoryId, out var category))
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
            TextureService.DrawIcon(new GameIconLookup(item.Icon, isHq), IconSize * ImGuiHelpers.GlobalScale);

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

            ImGui.TextUnformatted(TextService.GetItemName(item.RowId));
        }

        ImGuiContextMenuService.Draw(popupKey, builder =>
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
        Tweak!.UpdatePending = MirageManager.Instance()->RestorePrismBoxItem(itemIndex);
    }
}
