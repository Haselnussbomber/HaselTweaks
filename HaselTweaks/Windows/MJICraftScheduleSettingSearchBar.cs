using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselTweaks.Windows;

public unsafe class MJICraftScheduleSettingSearchBar : SimpleWindow
{
    private static AddonMJICraftScheduleSetting* Addon => GetAddon<AddonMJICraftScheduleSetting>("MJICraftScheduleSetting");
    private EnhancedIsleworksAgendaConfiguration Config => PluginConfig.Tweaks.EnhancedIsleworksAgenda;

    private const int LanguageSelectorWidth = 90;

    private readonly PluginConfig PluginConfig;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private bool InputFocused;
    private string Query = string.Empty;

    public MJICraftScheduleSettingSearchBar(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        ExcelService excelService,
        TextService textService)
        : base(windowManager, "MJICraftScheduleSetting Search Bar")
    {
        PluginConfig = pluginConfig;
        ExcelService = excelService;
        TextService = textService;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override bool DrawConditions()
        => Addon != null && Addon->AtkUnitBase.IsVisible;

    public override void OnOpen()
    {
        InputFocused = false;
        Query = string.Empty;
    }

    public override void Draw()
    {
        if (!InputFocused)
        {
            ImGui.SetKeyboardFocusHere(0);
            InputFocused = true;
        }

        var lastQuery = Query;
        var contentRegionAvail = ImGui.GetContentRegionAvail();

        ImGui.SetNextItemWidth(contentRegionAvail.X - LanguageSelectorWidth * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.InputTextWithHint("##Query", TextService.Translate("EnhancedIsleworksAgenda.MJICraftScheduleSettingSearchBar.QueryHint"), ref Query, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var evt = new AtkEvent();
            Addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, 6, &evt);
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(LanguageSelectorWidth * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language", Enum.GetName(Config.SearchLanguage) ?? "Language..."))
        {
            if (dropdown)
            {
                var values = Enum.GetValues<ClientLanguage>().OrderBy((ClientLanguage lang) => lang.ToString());
                foreach (var value in values)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == Config.SearchLanguage))
                    {
                        Config.SearchLanguage = value;
                        PluginConfig.Save();
                    }
                }
            }
        }

        if (lastQuery != Query)
        {
            var entries = new List<(int Index, string ItemName)>();
            for (var i = 0; i < Addon->TreeList->Items.LongCount; i++)
            {
                var item = Addon->TreeList->Items[i].Value;
                if (item != null && item->UIntValues.LongCount >= 3 && item->UIntValues[0] != (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                {
                    var rowId = item->UIntValues[2];

                    if (!ExcelService.TryGetRow<MJICraftworksObject>(rowId, out var mjiCraftworksObject))
                        continue;

                    if (!ExcelService.TryGetRow<Item>(mjiCraftworksObject.Item.RowId, Config.SearchLanguage, out var itemRow))
                        continue;

                    var itemName = itemRow.Name.ExtractText();
                    if (string.IsNullOrEmpty(itemName))
                        continue;

                    entries.Add((i, itemName.ToLower()));
                }
            }

            var result = entries.FuzzyMatch(Query.ToLower().Trim(), value => value.ItemName).FirstOrDefault();
            if (result != default)
            {
                var index = result.Value.Index;
                var item = Addon->TreeList->GetItem(index);
                if (item != null)
                {
                    // find parent group and expand it
                    for (var i = index; i >= 0; i--)
                    {
                        var headerItem = Addon->TreeList->Items[i].Value;
                        if (headerItem != null && headerItem->UIntValues.LongCount >= 1 && headerItem->UIntValues[0] == (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                        {
                            Addon->TreeList->ExpandGroupExclusively(headerItem, false);
                            Addon->TreeList->LayoutRefreshPending = true;
                            break;
                        }
                    }

                    Addon->TreeList->SelectItem(index, true); // if it would only scroll the selected item into view... oh well
                }
            }
        }

        var scale = ImGuiHelpers.GlobalScale;
        var scaledown = 1 / scale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * scaledown;

        Position = new(
            Addon->AtkUnitBase.X + 4,
            Addon->AtkUnitBase.Y + 3 - height * scale
        );

        Size = new(
            (Addon->AtkUnitBase.GetScaledWidth(true) - 8) * scaledown,
            height
        );
    }
}
