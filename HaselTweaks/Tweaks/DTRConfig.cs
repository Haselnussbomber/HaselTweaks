using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using HaselCommon.Utils;
using HaselTweaks.Config;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public class DTRConfiguration
{
    public string FormatUnitText = " fps";
}

public partial class DTR
{
    private DTRConfiguration Config => PluginConfig.Tweaks.DTR;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();

        TextService.Draw("DTR.Config.Explanation.Pre");
        TextService.Draw(HaselColor.From(ImGuiColors.DalamudRed), "DTR.Config.Explanation.DalamudSettings");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        if (ImGui.IsItemClicked())
        {
            void OpenSettings()
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    Framework.RunOnTick(OpenSettings, delayTicks: 2);
                    return;
                }

                DalamudPluginInterface.OpenDalamudSettingsTo(SettingsOpenKind.ServerInfoBar);
            }
            Framework.RunOnTick(OpenSettings, delayTicks: 2);
        }
        ImGuiUtils.SameLineSpace();
        TextService.Draw("DTR.Config.Explanation.Post");

        ImGuiUtils.DrawSection(TextService.Translate("HaselTweaks.Config.SectionTitle.Configuration"));

        TextService.Draw("DTR.Config.FormatUnitText.Label");
        if (ImGui.InputText("##FormatUnitTextInput", ref Config.FormatUnitText, 20))
        {
            PluginConfig.Save();
            _lastFrameRate = 0; // trigger update
        }
        ImGui.SameLine();
        if (ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, TextService.Translate("HaselTweaks.Config.ResetToDefault", " fps")))
        {
            Config.FormatUnitText = " fps";
            PluginConfig.Save();
        }

        if (TextService.TryGetTranslation("DTR.Config.FormatUnitText.Description", out var description))
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }
}
