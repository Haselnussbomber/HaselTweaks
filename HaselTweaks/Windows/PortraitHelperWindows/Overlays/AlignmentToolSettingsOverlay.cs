using HaselTweaks.Enums.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterTransient, AutoConstruct]
public unsafe partial class AlignmentToolSettingsOverlay : Overlay
{
    private readonly TextService _textService;
    private readonly PluginConfig _pluginConfig;

    public override OverlayType Type => OverlayType.LeftPane;

    public override void Draw()
    {
        base.Draw();

        var config = _pluginConfig.Tweaks.PortraitHelper;

        ImGuiUtils.DrawSection(
            _textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title.Inner"),
            pushDown: false,
            respectUiTheme: !IsWindow);

        var changed = false;

        changed |= ImGui.Checkbox(_textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.ShowAlignmentTool.Label"), ref config.ShowAlignmentTool);

        using var _ = ImRaii.Disabled(!config.ShowAlignmentTool);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text(_textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.VerticalLines.Label"));
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Vertical Lines", ref config.AlignmentToolVerticalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Vertical Color", ref config.AlignmentToolVerticalColor);

        ImGui.Unindent();
        ImGui.Text(_textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.HorizontalLines.Label"));
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Horizontal Lines", ref config.AlignmentToolHorizontalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Horizontal Color", ref config.AlignmentToolHorizontalColor);

        ImGui.Unindent();

        if (changed)
        {
            _pluginConfig.Save();
        }
    }
}
