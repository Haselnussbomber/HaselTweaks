using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Data.Files;

namespace HaselTweaks.Utils;

public static class ImGuiUtils
{
    public static Vector4 ColorWhite { get; } = Vector4.One;
    public static Vector4 ColorOrange { get; } = new(1f, 0.6f, 0f, 1f);
    public static Vector4 ColorGold { get; } = new(0.847f, 0.733f, 0.49f, 1f);
    public static Vector4 ColorGreen { get; } = new(0f, 1f, 0f, 1f);
    public static Vector4 ColorRed { get; } = new(1f, 0f, 0f, 1f);
    public static Vector4 ColorGrey { get; } = new(0.73f, 0.73f, 0.73f, 1f);
    public static Vector4 ColorGrey2 { get; } = new(0.87f, 0.87f, 0.87f, 1f);
    public static Vector4 ColorGrey3 { get; } = new(0.6f, 0.6f, 0.6f, 1f);

    private static readonly Dictionary<int, TextureWrap> icons = new();

    public static void DrawIcon(int iconId, int width = -1, int height = -1)
    {
        if (!icons.ContainsKey(iconId))
        {
            var tex = Service.Data.GetFile<TexFile>($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex");
            if (tex == null)
                return;

            var texWrap = Service.PluginInterface.UiBuilder.LoadImageRaw(tex.GetRgbaImageData(), tex.Header.Width, tex.Header.Height, 4);
            if (texWrap.ImGuiHandle == IntPtr.Zero)
                return;

            icons[iconId] = texWrap;
        }

        ImGui.Image(icons[iconId].ImGuiHandle, new(width == -1 ? icons[iconId].Width : width, height == -1 ? icons[iconId].Height : height));
    }

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
            ImGui.BeginTooltip();
            ImGui.Text(title);

            var pos = ImGui.GetCursorPos();
            ImGui.GetWindowDrawList().AddText(
                UiBuilder.IconFont, 12,
                ImGui.GetWindowPos() + pos + new Vector2(2),
                ImGui.GetColorU32(ColorGrey),
                FontAwesomeIcon.ExternalLinkAlt.ToIconString()
            );
            ImGui.SetCursorPos(pos + new Vector2(20, 0));
            ImGui.TextColored(ColorGrey, url);
            ImGui.EndTooltip();
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

    public static void SameLineSpace()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.CalcTextSize(" ").X, 0));
        ImGui.SameLine();
        ImGui.PopStyleVar();
    }

    public static void TextColoredWrapped(Vector4 col, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(col));
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    public static bool IconButton(FontAwesomeIcon icon, Vector2 size = default)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var ret = ImGui.Button(icon.ToIconString(), size);
        ImGui.PopFont();
        return ret;
    }

    public static bool IconButton(FontAwesomeIcon icon, string key, Vector2 size = default)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var ret = ImGui.Button(icon.ToIconString() + key, size);
        ImGui.PopFont();
        return ret;
    }
}
