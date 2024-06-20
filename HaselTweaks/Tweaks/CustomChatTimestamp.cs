using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Structs;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

public sealed class CustomChatTimestampConfiguration
{
    public string Format = "[HH:mm] ";
}

public sealed unsafe class CustomChatTimestamp(
    ILogger<CustomChatTimestamp> Logger,
    IGameInteropProvider GameInteropProvider,
    PluginConfig PluginConfig,
    TranslationManager TranslationManager,
    IGameConfig GameConfig)
    : Tweak<CustomChatTimestampConfiguration>(PluginConfig, TranslationManager)
{
    private Hook<HaselRaptureTextModule.Delegates.FormatAddonText2Int>? FormatAddonText2IntHook;

    public override void OnInitialize()
    {
        FormatAddonText2IntHook = GameInteropProvider.HookFromAddress<HaselRaptureTextModule.Delegates.FormatAddonText2Int>(
            HaselRaptureTextModule.MemberFunctionPointers.FormatAddonText2Int,
            FormatAddonText2IntDetour);
    }

    public override void OnEnable()
    {
        FormatAddonText2IntHook?.Enable();
        ReloadChat();
    }

    public override void OnDisable()
    {
        FormatAddonText2IntHook?.Disable();
        ReloadChat();
    }

    private byte* FormatAddonText2IntDetour(HaselRaptureTextModule* self, uint addonRowId, int value)
    {
        if (addonRowId is 7840 or 7841 && !string.IsNullOrWhiteSpace(Config.Format))
        {
            try
            {
                var str = ((RaptureTextModule*)self)->UnkStrings1.GetPointer(1);
                str->SetString(DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime().ToString(Config.Format));
                return str->StringPtr;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error formatting Chat Timestamp");
            }
        }

        return FormatAddonText2IntHook!.Original(self, addonRowId, value);
    }

    private static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }

    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        ImGui.TextUnformatted(t("CustomChatTimestamp.Config.Format.Label"));
        using (ImGuiUtils.ConfigIndent())
        {
            if (ImGui.InputText("##Format", ref Config.Format, 50))
            {
                PluginConfig.Save();
                ReloadChat();
            }
            ImGui.SameLine();
            if (ImGuiUtils.IconButton("##FormatReset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", "\"[HH:mm] \"")))
            {
                Config.Format = "[HH:mm] ";
                PluginConfig.Save();
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
}
