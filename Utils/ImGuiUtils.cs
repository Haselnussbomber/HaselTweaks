using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Utility;
using ImGuiNET;
using Windows.Win32;

namespace HaselTweaks.Utils;

// TODO: cleanup (well, thats a project-wide task tbh)
public static partial class ImGuiUtils
{
    public static Vector4 ColorTransparent { get; } = Vector4.Zero;
    public static Vector4 ColorWhite { get; } = Vector4.One;
    public static Vector4 ColorOrange { get; } = new(1f, 0.6f, 0f, 1f);
    public static Vector4 ColorGold { get; } = new(0.847f, 0.733f, 0.49f, 1f);
    public static Vector4 ColorGreen { get; } = new(0f, 1f, 0f, 1f);
    public static Vector4 ColorRed { get; } = new(1f, 0f, 0f, 1f);
    public static Vector4 ColorGrey { get; } = new(0.73f, 0.73f, 0.73f, 1f);
    public static Vector4 ColorGrey2 { get; } = new(0.87f, 0.87f, 0.87f, 1f);
    public static Vector4 ColorGrey3 { get; } = new(0.6f, 0.6f, 0.6f, 1f);

    public static void DrawPaddedSeparator()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y - 1);
    }

    public static void DrawSection(string label)
    {
        // push down a bit
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y * 2);
        TextUnformattedColored(ColorGold, label);
        // pull up the separator
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y * 2 - 1);
    }

    public static ImRaii.Indent ConfigIndent() => ImRaii.PushIndent(ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X / 2f);

    public static void DrawLink(string label, string title, string url)
    {
        ImGui.TextUnformatted(label);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            using var tooltip = ImRaii.Tooltip();
            if (tooltip != null && tooltip.Success)
            {
                TextUnformattedColored(ColorWhite, title);

                var pos = ImGui.GetCursorPos();
                ImGui.GetWindowDrawList().AddText(
                    UiBuilder.IconFont, 12,
                    ImGui.GetWindowPos() + pos + new Vector2(2),
                    ImGui.GetColorU32(ColorGrey),
                    FontAwesomeIcon.ExternalLinkAlt.ToIconString()
                );
                ImGui.SetCursorPos(pos + new Vector2(20, 0));
                TextUnformattedColored(ColorGrey, url);
            }
        }

        if (ImGui.IsItemClicked())
        {
            Task.Run(() => Util.OpenLink(url));
        }
    }

    public static void BulletSeparator()
    {
        ImGui.SameLine();
        ImGui.TextUnformatted("•");
        ImGui.SameLine();
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
        => VerticalSeparator(ColorGrey3);

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

        for (var i = 0; i < numSegments; i++)
        {
            var x = center.X + radius * (float)Math.Cos(angle);
            var y = center.Y + radius * (float)Math.Sin(angle);

            var t = (float)(-angle / (float)Math.PI / 2f + ImGui.GetTime()) % 1f;
            var color = new Vector4(1f, 1f, 1f, 1 - t);

            ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(x, y), 2f, ImGui.ColorConvertFloat4ToU32(color));

            angle += angleStep;
        }
    }

    public static void TextUnformattedDisabled(string text)
    {
        using (ImRaii.Disabled())
            ImGui.TextUnformatted(text);
    }

    public static void TextUnformattedColored(Vector4 col, string text)
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
