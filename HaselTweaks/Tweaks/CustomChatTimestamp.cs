using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.String;
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
public unsafe partial class CustomChatTimestamp : Tweak<CustomChatTimestampConfiguration>
{
    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.TextUnformatted(t("CustomChatTimestamp.Config.Format.Label"));
        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.InputText("##Format", ref Config.Format, 50))
            {
                Plugin.Config.Save();
                ReloadChat();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##FormatReset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", "\"[HH:mm] \"")))
            {
                Config.Format = "[HH:mm] ";
                Plugin.Config.Save();
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
                // TODO: check this: colorParty = ImColor.FromABGR(colorParty); 
                var alpha = (colorParty & 0xFF000000) >> 24;
                var red = (colorParty & 0x00FF0000) >> 16;
                var green = (colorParty & 0x0000FF00) >> 8;
                var blue = colorParty & 0x000000FF;

                colorParty = (alpha << 24) | (blue << 16) | (green << 8) | red;
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

    [AddressHook<HaselRaptureTextModule>(nameof(HaselRaptureTextModule.Addresses.FormatAddonInt))]
    private byte* FormatAddon(nint a1, ulong addonRowId, ulong value)
    {
        if (addonRowId is 7840 or 7841 && !string.IsNullOrWhiteSpace(Config.Format))
        {
            try
            {
                var time = DateTime.UnixEpoch.AddSeconds(value).ToLocalTime();
                var formatted = time.ToString(Config.Format);

                var str = (Utf8String*)(a1 + 0x9C0);
                str->SetString(formatted);
                return str->StringPtr;
            }
            catch (Exception e)
            {
                Error(e, "Error formatting Chat Timestamp");
            }
        }

        return FormatAddonHook.OriginalDisposeSafe(a1, addonRowId, value);
    }

    public static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabsPendingReload[i] = 1;
    }
}
