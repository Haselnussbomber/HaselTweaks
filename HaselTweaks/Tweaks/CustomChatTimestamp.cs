using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public class CustomChatTimestampConfiguration
{
    public string Format = "[HH:mm] ";
}

[Tweak]
public partial class CustomChatTimestamp : Tweak<CustomChatTimestampConfiguration>
{
    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.TextUnformatted(t("CustomChatTimestamp.Config.Format.Label"));
        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.InputText("##Format", ref Config.Format, 50))
            {
                Service.GetService<Configuration>().Save();
                ReloadChat();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##FormatReset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", "\"[HH:mm] \"")))
            {
                Config.Format = "[HH:mm] ";
                Service.GetService<Configuration>().Save();
                ReloadChat();
            }

            ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Grey);
            ImGui.TextUnformatted(t("CustomChatTimestamp.Config.Format.DateTimeLink.Pre"));
            ImGuiUtils.SameLineSpace();
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.White))
            {
                ImGuiUtils.DrawLink("DateTime.ToString()", t("CustomChatTimestamp.Config.Format.DateTimeLink.Tooltip"), "https://docs.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings");
            }
            ImGuiUtils.SameLineSpace();
            ImGui.TextUnformatted(t("CustomChatTimestamp.Config.Format.DateTimeLink.Post"));
            ImGui.PopStyleColor();
        }

        if (string.IsNullOrWhiteSpace(Config.Format))
            return;

        try
        {
            var formatted = DateTime.Now.ToString(Config.Format);

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.TextUnformatted(t("CustomChatTimestamp.Config.Format.Example.Label"));

            if (!Service.GameConfig.UiConfig.TryGet("ColorParty", out uint colorParty))
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
            if (!child.Success)
                return;

            ImGuiUtils.TextUnformattedColored(Colors.White, formatted);
            ImGui.SameLine(0, 0);
            ImGuiUtils.TextUnformattedColored(colorParty, t("CustomChatTimestamp.Config.Format.Example.Message"));
        }
        catch (FormatException)
        {
            using (ImRaii.PushIndent())
            {
                ImGuiHelpers.SafeTextColoredWrapped(Colors.Red, t("CustomChatTimestamp.Config.Format.Invalid"));
            }
        }
        catch (Exception e)
        {
            using (ImRaii.PushIndent())
            {
                ImGuiHelpers.SafeTextColoredWrapped(Colors.Red, e.Message);
            }
        }
    }

    public override void Enable()
    {
        ReloadChat();
    }

    public override void Disable()
    {
        ReloadChat();
    }

    [AddressHook<HaselRaptureTextModule>(nameof(HaselRaptureTextModule.Addresses.FormatAddonText2Int))]
    public unsafe byte* FormatAddonText2Int(RaptureTextModule* raptureTextModule, uint addonRowId, int value)
    {
        if (addonRowId is 7840 or 7841 && !string.IsNullOrWhiteSpace(Config.Format))
        {
            try
            {
                raptureTextModule->Unk9C0.SetString(DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime().ToString(Config.Format));
                return raptureTextModule->Unk9C0.StringPtr;
            }
            catch (Exception e)
            {
                Error(e, "Error formatting Chat Timestamp");
            }
        }

        return FormatAddonText2IntHook.OriginalDisposeSafe(raptureTextModule, addonRowId, value);
    }

    public static unsafe void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }
}
