using System.Collections.Generic;
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

    public static bool IconButton(string key, FontAwesomeIcon icon, string tooltip, Vector2 size = default, bool disabled = false, bool active = false)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        if (!key.StartsWith("##")) key = "##" + key;

        var disposables = new List<IDisposable>();

        if (disabled)
        {
            disposables.Add(ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]));
            disposables.Add(ImRaii.PushColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]));
            disposables.Add(ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]));
        }
        else if (active)
        {
            disposables.Add(ImRaii.PushColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]));
        }

        var pressed = ImGui.Button(icon.ToIconString() + key, size);

        foreach (var disposable in disposables)
            disposable.Dispose();

        iconFont?.Dispose();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        return pressed;
    }
}
