using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Gui;
using HaselTweaks.Config;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public class CustomChatTimestampConfiguration
{
    public string Format = "[HH:mm] ";
}

public partial class CustomChatTimestamp
{
    private CustomChatTimestampConfiguration Config => PluginConfig.Tweaks.CustomChatTimestamp;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();

        TextService.Draw("CustomChatTimestamp.Config.Format.Label");
        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.InputText("##Format", ref Config.Format, 50))
            {
                PluginConfig.Save();
                ReloadChat();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##FormatReset", FontAwesomeIcon.Undo, TextService.Translate("HaselTweaks.Config.ResetToDefault", "\"[HH:mm] \"")))
            {
                Config.Format = "[HH:mm] ";
                PluginConfig.Save();
                ReloadChat();
            }

            ImGui.PushStyleColor(ImGuiCol.Text, (uint)Color.Grey);
            TextService.Draw("CustomChatTimestamp.Config.Format.DateTimeLink.Pre");
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.White))
            {
                ImGuiUtils.DrawLink("DateTime.ToString()", TextService.Translate("CustomChatTimestamp.Config.Format.DateTimeLink.Tooltip"), "https://docs.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings");
            }
            ImGuiUtils.SameLineSpace();
            TextService.Draw("CustomChatTimestamp.Config.Format.DateTimeLink.Post");
            ImGui.PopStyleColor();
        }

        if (string.IsNullOrWhiteSpace(Config.Format))
            return;

        try
        {
            var formatted = DateTime.Now.ToString(Config.Format);

            ImGui.Spacing();
            TextService.Draw("CustomChatTimestamp.Config.Format.Example.Label");

            if (!GameConfig.UiConfig.TryGet("ColorParty", out uint colorParty))
            {
                colorParty = 0xFFFFE666;
            }
            else
            {
                //var alpha = (colorParty & 0xFF000000) >> 24;
                var red = (colorParty & 0x00FF0000) >> 16;
                var green = (colorParty & 0x0000FF00) >> 8;
                var blue = colorParty & 0x000000FF;

                colorParty = 0xFF000000u | (blue << 16) | (green << 8) | red;
            }

            var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetTextLineHeight() + 2);
            using var child = ImRaii.Child("##FormatExample", size, true);
            if (!child) return;

            ImGuiUtils.TextUnformattedColored(Color.White, formatted);
            ImGui.SameLine(0, 0);
            TextService.Draw(Color.From(colorParty), "CustomChatTimestamp.Config.Format.Example.Message");
        }
        catch (FormatException)
        {
            using (ImRaii.PushIndent())
            {
                TextService.DrawWrapped(Color.Red, "CustomChatTimestamp.Config.Format.Invalid");
            }
        }
        catch (Exception e)
        {
            using (ImRaii.PushIndent())
            {
                ImGuiHelpers.SafeTextColoredWrapped(Color.Red, e.Message);
            }
        }
    }
}
