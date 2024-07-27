using Dalamud.Interface.Utility.Raii;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Enums.PortraitHelper;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AlignmentToolSettingsOverlay(
    TextService TextService,
    WindowManager windowManager,
    PluginConfig pluginConfig,
    ExcelService excelService)
    : Overlay(
        windowManager,
        pluginConfig,
        excelService,
        TextService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title"))
{
    public override OverlayType Type => OverlayType.LeftPane;

    public override void Draw()
    {
        base.Draw();

        ImGuiUtils.DrawSection(
            TextService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title.Inner"),
            PushDown: false,
            RespectUiTheme: !IsWindow);

        var changed = false;

        changed |= ImGui.Checkbox(TextService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.ShowAlignmentTool.Label"), ref Config.ShowAlignmentTool);

        using var _ = ImRaii.Disabled(!Config.ShowAlignmentTool);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        TextService.Draw("PortraitHelperWindows.AlignmentToolSettingsOverlay.VerticalLines.Label");
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Vertical Lines", ref Config.AlignmentToolVerticalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Vertical Color", ref Config.AlignmentToolVerticalColor);

        ImGui.Unindent();
        TextService.Draw("PortraitHelperWindows.AlignmentToolSettingsOverlay.HorizontalLines.Label");
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Horizontal Lines", ref Config.AlignmentToolHorizontalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Horizontal Color", ref Config.AlignmentToolHorizontalColor);

        ImGui.Unindent();

        if (changed)
        {
            PluginConfig.Save();
        }
    }
}
