using HaselTweaks.Enums.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterScoped, AutoConstruct]
public unsafe partial class AlignmentToolSettingsOverlay : Overlay
{
    private readonly TextService _textService;
    private readonly PluginConfig _pluginConfig;

    public override OverlayType Type => OverlayType.LeftPane;

    public override void Draw()
    {
        base.Draw();

        ImGuiUtils.DrawSection(
            _textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title.Inner"),
            pushDown: false,
            respectUiTheme: !IsWindow);

        var changed = false;

        changed |= ImGui.Checkbox(_textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.ShowAlignmentTool.Label"), ref Config.ShowAlignmentTool);

        using var _ = ImRaii.Disabled(!Config.ShowAlignmentTool);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.VerticalLines.Label"));
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Vertical Lines", ref Config.AlignmentToolVerticalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Vertical Color", ref Config.AlignmentToolVerticalColor);

        ImGui.Unindent();
        ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.AlignmentToolSettingsOverlay.HorizontalLines.Label"));
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Horizontal Lines", ref Config.AlignmentToolHorizontalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Horizontal Color", ref Config.AlignmentToolHorizontalColor);

        ImGui.Unindent();

        if (changed)
        {
            _pluginConfig.Save();
        }
    }
}
