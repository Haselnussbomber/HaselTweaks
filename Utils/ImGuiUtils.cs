using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;

namespace HaselTweaks.Utils;

public static class ImGuiUtils
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

    public static readonly Dictionary<int, TextureWrap?> IconCache = new();

    public static void DrawIcon(int iconId, float width = -1, float height = -1)
    {
        if (!IconCache.TryGetValue(iconId, out var tex))
        {
            tex = Service.Data.GetImGuiTexture($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex");
            IconCache[iconId] = tex;
        }

        if (tex == null || tex.ImGuiHandle == 0)
            return;

        ImGui.Image(tex.ImGuiHandle, new(width == -1 ? tex.Width : width, height == -1 ? tex.Height : height));
    }

    public static void DrawIcon(int iconId, Vector2 size)
        => DrawIcon(iconId, size.X, size.Y);

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
        ImGui.TextColored(ColorGold, label);
        // pull up the separator
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y * 2 - 1);
    }

    public static void DrawLink(string label, string title, string url)
    {
        ImGui.Text(label);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            using var tooltip = ImRaii.Tooltip();
            if (tooltip != null && tooltip.Success)
            {
                ImGui.TextColored(ColorWhite, title);

                var pos = ImGui.GetCursorPos();
                ImGui.GetWindowDrawList().AddText(
                    UiBuilder.IconFont, 12,
                    ImGui.GetWindowPos() + pos + new Vector2(2),
                    ImGui.GetColorU32(ColorGrey),
                    FontAwesomeIcon.ExternalLinkAlt.ToIconString()
                );
                ImGui.SetCursorPos(pos + new Vector2(20, 0));
                ImGui.TextColored(ColorGrey, url);
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
        ImGui.Text("•");
        ImGui.SameLine();
    }

    public static unsafe void VerticalSeparator(Vector4 color)
    {
        ImGui.SameLine();

        var region = ImGui.GetContentRegionAvail();
        var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();

        ImGui.GetWindowDrawList().AddLine(
            pos + new Vector2(3f, 0f),
            pos + new Vector2(3f, region.Y),
            ImGui.GetColorU32(color)
        );
        ImGui.Dummy(new(7, region.Y));
    }

    public static unsafe void VerticalSeparator()
        => VerticalSeparator(ColorGrey3);

    public static void SameLineSpace()
    {
        using var itemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.CalcTextSize(" ").X, 0));
        ImGui.SameLine();
    }

    public static void TextColoredWrapped(Vector4 col, string text)
    {
        using var textCol = ImRaii.PushColor(ImGuiCol.Text, col);
        ImGui.TextWrapped(text);
    }

    public static unsafe bool ButtonDisabled(string label, Vector2 size = default)
    {
        using (ImRaii.Disabled())
        {
            return ImGui.Button(label, size);
        }
    }

    #region IconButton

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

    public static bool IconButton(FontAwesomeIcon icon, Vector2 size = default)
        => IconButton("", icon, size);

    public static bool IconButton(FontAwesomeIcon icon, string tooltip, Vector2 size = default)
        => IconButton("", icon, tooltip, size);

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
        var pressed = IconButtonDisabled(key, icon, size);

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
