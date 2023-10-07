using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using ImGuiNET;

namespace HaselTweaks.Windows;

public unsafe class MJICraftScheduleSettingSearchBar : Window
{
    private static AddonMJICraftScheduleSetting* Addon => GetAddon<AddonMJICraftScheduleSetting>("MJICraftScheduleSetting");
    private bool InputFocused;
    private string Query = string.Empty;

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

        ImGui.SetNextItemWidth(-1);
        var lastQuery = Query;

        if (ImGui.InputTextWithHint("##Query", t("EnhancedIsleworksAgenda.MJICraftScheduleSettingSearchBar.QueryHint"), ref Query, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var evt = stackalloc AtkEvent[1];
            Addon->AtkUnitBase.ReceiveEvent(AtkEventType.ButtonClick, 6, evt);
        }

        if (lastQuery != Query)
        {
            var entries = new List<(uint Index, string ItemName)>();
            for (var i = 0u; i < Addon->TreeList->Items.Size(); i++)
            {
                var item = Addon->TreeList->Items.Get(i).Value;
                if (item != null && item->StringValues.Size() > 0 && item->UIntValues.Size() > 0 && item->UIntValues.Get(0) != (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                {
                    var titlePtr = item->StringValues.Get(0).Value;
                    if (titlePtr != null)
                    {
                        var itemName = MemoryHelper.ReadStringNullTerminated((nint)titlePtr);
                        if (!string.IsNullOrEmpty(itemName))
                        {
                            entries.Add((i, itemName.ToLower()));
                        }
                    }
                }
            }

            var result = FuzzySharp.Process.ExtractTop((Index: 0u, ItemName: Query.ToLower()), entries, (entry) => entry.ItemName, limit: 1).FirstOrDefault();
            if (result != null && result.Value != default)
            {
                var index = result.Value.Index;
                var item = Addon->TreeList->GetItem(index);
                if (item != null)
                {
                    // find parent group and expand it
                    for (var i = index; i >= 0; i--)
                    {
                        var headerItem = Addon->TreeList->Items.Get(i).Value;
                        if (headerItem != null && headerItem->UIntValues.Size() > 0 && headerItem->UIntValues.Get(0) == (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                        {
                            Addon->TreeList->ExpandGroup(headerItem, false);
                            Addon->TreeList->LayoutRefreshPending = 1;
                            break;
                        }
                    }

                    Addon->TreeList->SelectItem(index, true); // if it would only scroll the selected item into view... oh well
                }
            }
        }

        var scale = ImGui.GetIO().FontGlobalScale;
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
