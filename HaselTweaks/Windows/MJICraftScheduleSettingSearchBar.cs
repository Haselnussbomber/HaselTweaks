using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows;

public unsafe class MJICraftScheduleSettingSearchBar : Window
{
    private static AddonMJICraftScheduleSetting* Addon => GetAddon<AddonMJICraftScheduleSetting>("MJICraftScheduleSetting");
    private bool InputFocused;
    private string Query = string.Empty;
    private const int LanguageSelectorWidth = 90;

    private static EnhancedIsleworksAgendaConfiguration Config => Service.GetService<Configuration>().Tweaks.EnhancedIsleworksAgenda;

    public MJICraftScheduleSettingSearchBar() : base("MJICraftScheduleSetting Search Bar")
    {
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;
        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override bool DrawConditions()
        => Addon != null && Addon->AtkUnitBase.IsVisible;

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
        if (ImGui.InputTextWithHint("##Query", t("EnhancedIsleworksAgenda.MJICraftScheduleSettingSearchBar.QueryHint"), ref Query, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var evt = stackalloc AtkEvent[1];
            Addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, 6, evt);
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
                        Service.GetService<Configuration>().Save();
                    }
                }
            }
        }

        if (lastQuery != Query)
        {
            var entries = new List<(uint Index, string ItemName)>();
            for (var i = 0u; i < Addon->TreeList->Items.LongCount; i++)
            {
                var item = Addon->TreeList->Items[i].Value;
                if (item != null && item->UIntValues.LongCount >= 3 && item->UIntValues[0] != (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                {
                    var rowId = item->UIntValues[2];
                    var itemId = GetRow<MJICraftworksObject>(rowId)?.Item.Row ?? 0;
                    if (itemId == 0)
                        continue;

                    var itemName = GetSheet<Item>(Config.SearchLanguage)?.GetRow(itemId)?.Name.ToDalamudString().ToString();
                    if (string.IsNullOrEmpty(itemName))
                        continue;

                    entries.Add((i, itemName.ToLower()));
                }
            }

            var result = entries.FuzzyMatch(Query.ToLower(), value => value.ItemName).FirstOrDefault();
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

                    Addon->TreeList->SelectItem((int)index, true); // if it would only scroll the selected item into view... oh well
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
