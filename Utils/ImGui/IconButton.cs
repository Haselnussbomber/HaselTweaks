using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using ImGuiNET;

namespace HaselTweaks.Utils;

public partial class ImGuiUtils
{
    public static Vector2 GetIconButtonSize(FontAwesomeIcon icon)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        return ImGui.CalcTextSize(icon.ToIconString()) + ImGui.GetStyle().FramePadding * 2;
    }

    public static bool IconButton(string key, FontAwesomeIcon icon, string tooltip, Vector2 size = default, bool disabled = false)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        if (!key.StartsWith("##")) key = "##" + key;

        var a = disabled ? ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]) : null;
        var b = disabled ? ImRaii.PushColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]) : null;
        var c = disabled ? ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]) : null;

        var pressed = ImGui.Button(icon.ToIconString() + key, size);

        c?.Dispose();
        b?.Dispose();
        a?.Dispose();
        iconFont?.Dispose();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        return pressed;
    }
}
