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
            var entries = new List<string>();
            foreach (var ptr in Addon->TreeList->Items.Span)
            {
                var item = ptr.Value;
                if (item != null)
                {
                    var itemName = MemoryHelper.ReadStringNullTerminated(*(nint*)item->Title);
                    entries.Add(itemName.ToLower());
                }
            }

            var result = FuzzySharp.Process.ExtractTop(Query.ToLower(), entries, (entry) => entry, limit: 1).First();
            if (result.Index >= 0 && result.Index < entries.Count)
            {
                var item = Addon->TreeList->GetItem((uint)result.Index);
                if (item != null && item->Data->Type != AtkComponentTreeListItemType.CollapsibleGroupHeader)
                {
                    var evt = stackalloc AtkEvent[1];
                    var a5 = stackalloc uint[4];
                    a5[4] = (uint)result.Index;
                    Addon->AtkUnitBase.ReceiveEvent(AtkEventType.ListItemToggle, 1, evt, (nint)a5);
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
