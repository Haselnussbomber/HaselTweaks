using System;
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
    public const uint ColorWhite = 0xFFFFFFFF;
    public const uint ColorOrange = 0xFF009AFF;
    public const uint ColorGold = 0xFF7DBBD8;
    public const uint ColorGreen = 0xFF00FF00;
    public const uint ColorRed = 0xFF0000FF;
    public const uint ColorLightRed = 0xFF3333DD;
    public const uint ColorGrey = 0xFFBBBBBB;
    public const uint ColorGrey2 = 0xFFDDDDDD;
    public const uint ColorGrey3 = 0xFF999999;

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

        ImGui.PushStyleColor(ImGuiCol.Text, ColorGold);
        ImGui.Text(label);
        ImGui.PopStyleColor();

        // pull up the separator
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y + 3);
        ImGui.Separator();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y * 2 - 1);
    }

    public static uint ToColor(this Vector4 vec)
    {
        return ((uint)(vec.W * 255) << 24) + ((uint)(vec.X * 255) << 16) + ((uint)(vec.Y * 255) << 8) + (uint)(vec.Z * 255);
    }

    public static Vector4 ToVector(this uint val)
    {
        return new Vector4((byte)val / 255f, (byte)(val >> 8) / 255f, (byte)(val >> 16) / 255f, (byte)(val >> 24) / 255f);
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
                ColorGrey,
                FontAwesomeIcon.ExternalLinkAlt.ToIconString()
            );
            ImGui.SetCursorPos(pos + new Vector2(20, 0));
            ImGui.TextColored(ColorGrey.ToVector(), url);
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
}
