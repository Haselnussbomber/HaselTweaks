using Dalamud.Interface;
using Dalamud.Interface.Colors;
using HaselCommon.Utils;
using HaselTweaks.Config;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public class DTRConfiguration
{
    public string FpsFormat = "{0} fps";
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ConfigGui.DrawString("FpsFormat", ref Config.FpsFormat, "{0} fps");
    }
}
