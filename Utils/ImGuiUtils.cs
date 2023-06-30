using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Utility;
using ImGuiNET;
using Windows.Win32;
using HaselTweaks.Extensions;

namespace HaselTweaks.Utils;

// TODO: cleanup (well, thats a project-wide task tbh)
public static partial class ImGuiUtils
{
    public static void DrawPaddedSeparator()
    {
        var style = ImGui.GetStyle();
        PushCursorY(style.ItemSpacing.Y);
        ImGui.Separator();
        PushCursorY(style.ItemSpacing.Y - 1);
    }

    public static void DrawSection(string label)
    {
        var style = ImGui.GetStyle();
        // push down a bit
        PushCursorY(style.ItemSpacing.Y * 2);
        TextUnformattedColored(Colors.Gold, label);
        // pull up the separator
        PushCursorY(-style.ItemSpacing.Y + 3);
        ImGui.Separator();
        PushCursorY(style.ItemSpacing.Y * 2 - 1);
    }

    public static ImRaii.Indent ConfigIndent()
        => ImRaii.PushIndent(ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X / 2f);

    public static void DrawLink(string label, string title, string url)
    {
        ImGui.TextUnformatted(label);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            using var tooltip = ImRaii.Tooltip();
            if (tooltip != null && tooltip.Success)
            {
                TextUnformattedColored(Colors.White, title);

                var pos = ImGui.GetCursorPos();
                ImGui.GetWindowDrawList().AddText(
                    UiBuilder.IconFont, 12,
                    ImGui.GetWindowPos() + pos + new Vector2(2),
                    Colors.Grey,
                    FontAwesomeIcon.ExternalLinkAlt.ToIconString()
                );
                ImGui.SetCursorPos(pos + new Vector2(20, 0));
                TextUnformattedColored(Colors.Grey, url);
            }
        }

        if (ImGui.IsItemClicked())
        {
            Task.Run(() => Util.OpenLink(url));
        }
    }

    public static unsafe void VerticalSeparator(Vector4 color)
    {
        ImGui.SameLine();

        var height = ImGui.GetFrameHeight();
        var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();

        ImGui.GetWindowDrawList().AddLine(
            pos + new Vector2(3f, 0f),
            pos + new Vector2(3f, height),
            ImGui.GetColorU32(color)
        );
        ImGui.Dummy(new(7, height));
    }

    public static unsafe void VerticalSeparator()
        => VerticalSeparator(Colors.Grey3);

    public static void SameLineSpace()
    {
        using var itemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.CalcTextSize(" ").X, 0));
        ImGui.SameLine();
    }

    public static void PushCursorX(float x)
        => ImGui.SetCursorPosX(ImGui.GetCursorPosX() + x);

    public static void PushCursorY(float y)
        => ImGui.SetCursorPosY(ImGui.GetCursorPosY() + y);

    public static bool IsGameWindowFocused()
        => Process.GetCurrentProcess().MainWindowHandle == PInvoke.GetForegroundWindow();

    public static void DrawLoadingSpinner(Vector2 center, float radius = 10f)
    {
        var angle = 0.0f;
        var numSegments = 10;
        var angleStep = (float)(Math.PI * 2.0f / numSegments);
        var time = ImGui.GetTime();
        var drawList = ImGui.GetWindowDrawList();

        for (var i = 0; i < numSegments; i++)
        {
            var pos = center + new Vector2(
                radius * (float)Math.Cos(angle),
                radius * (float)Math.Sin(angle));

            var t = (float)(-angle / (float)Math.PI / 2f + time) % 1f;
            var color = new Vector4(1f, 1f, 1f, 1 - t);

            drawList.AddCircleFilled(pos, 2f, ImGui.ColorConvertFloat4ToU32(color));

            angle += angleStep;
        }
    }

    public static void TextUnformattedDisabled(string text)
    {
        using (ImRaii.Disabled())
            ImGui.TextUnformatted(text);
    }

    public static void TextUnformattedColored(uint col, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, col))
            ImGui.TextUnformatted(text);
    }

    public static unsafe bool ButtonDisabled(string label, Vector2 size = default)
    {
        using (ImRaii.Disabled())
        {
            return ImGui.Button(label, size);
        }
    }

    #region IconButton

    public static Vector2 GetIconButtonSize(FontAwesomeIcon icon)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        return ImGui.CalcTextSize(icon.ToIconString()) + ImGui.GetStyle().FramePadding * 2;
    }

    public static bool IconButton(string key, FontAwesomeIcon icon, Vector2 size)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        if (!key.StartsWith("##")) key = "##" + key;
        return ImGui.Button(icon.ToIconString() + key, size);
    }

    public static bool IconButton(string key, FontAwesomeIcon icon, string tooltip, Vector2 size = default)
    {
        var pressed = IconButton(key, icon, size);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        return pressed;
    }

    public static bool IconButton(string key, FontAwesomeIcon icon)
        => IconButton(key, icon, default);

    #endregion

    #region IconButtonDisabled

    public static bool IconButtonDisabled(string key, FontAwesomeIcon icon, Vector2 size)
    {
        using var iconFont = ImRaii.PushFont(UiBuilder.IconFont);
        if (!key.StartsWith("##")) key = "##" + key;
        return ButtonDisabled(icon.ToIconString() + key, size);
    }

    public static bool IconButtonDisabled(string key, FontAwesomeIcon icon, string tooltip, Vector2 size = default)
    {
        var pressed = false;

        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]))
            pressed = IconButton(key, icon, size);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        return pressed;
    }

    public static bool IconButtonDisabled(string key, FontAwesomeIcon icon)
        => IconButtonDisabled(key, icon, default);

    public static bool IconButtonDisabled(FontAwesomeIcon icon, Vector2 size = default)
        => IconButtonDisabled("", icon, size);

    public static bool IconButtonDisabled(FontAwesomeIcon icon, string tooltip, Vector2 size = default)
        => IconButtonDisabled("", icon, tooltip, size);

    #endregion
}
