using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Gui;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public class CustomChatTimestampConfiguration
{
    public string Format = "[HH:mm] ";
}

public partial class CustomChatTimestamp
{
    private CustomChatTimestampConfiguration Config => _pluginConfig.Tweaks.CustomChatTimestamp;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["CustomTimestampFormat"])]);

        _configGui.DrawConfigurationHeader();

        ImGui.TextUnformatted(_textService.Translate("CustomChatTimestamp.Config.Format.Label"));
        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.InputText("##Format", ref Config.Format, 50))
            {
                _pluginConfig.Save();
                ReloadChat();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##FormatReset", FontAwesomeIcon.Undo, _textService.Translate("HaselTweaks.Config.ResetToDefault", "\"[HH:mm] \"")))
            {
                Config.Format = "[HH:mm] ";
                _pluginConfig.Save();
                ReloadChat();
            }

            ImGui.PushStyleColor(ImGuiCol.Text, (uint)Color.Grey);
            ImGui.TextUnformatted(_textService.Translate("CustomChatTimestamp.Config.Format.DateTimeLink.Pre"));
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.White))
            {
                ImGuiUtils.DrawLink("DateTime.ToString()", _textService.Translate("CustomChatTimestamp.Config.Format.DateTimeLink.Tooltip"), "https://docs.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings");
            }
            ImGuiUtils.SameLineSpace();
            ImGui.TextUnformatted(_textService.Translate("CustomChatTimestamp.Config.Format.DateTimeLink.Post"));
            ImGui.PopStyleColor();
        }

        if (string.IsNullOrWhiteSpace(Config.Format))
            return;

        try
        {
            var formatted = DateTime.Now.ToString(Config.Format);

            ImGui.Spacing();
            ImGui.TextUnformatted(_textService.Translate("CustomChatTimestamp.Config.Format.Example.Label"));

            if (!_gameConfig.UiConfig.TryGet("ColorParty", out uint colorParty))
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
            ImGuiHelpers.SafeTextColoredWrapped(Color.From(colorParty), _textService.Translate("CustomChatTimestamp.Config.Format.Example.Message"));
        }
        catch (FormatException)
        {
            using var indent = ImRaii.PushIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Color.Red, _textService.Translate("CustomChatTimestamp.Config.Format.Invalid"));
        }
        catch (Exception e)
        {
            using var indent = ImRaii.PushIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Color.Red, e.Message);
        }
    }
}
