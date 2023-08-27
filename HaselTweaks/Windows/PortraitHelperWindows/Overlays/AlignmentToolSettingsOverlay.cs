using Dalamud.Interface.Raii;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AlignmentToolSettingsOverlay : Overlay
{
    protected override OverlayType Type => OverlayType.LeftPane;

    public AlignmentToolSettingsOverlay() : base(t("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title"))
    {
    }

    public override void Draw()
    {
        base.Draw();

        ImGuiUtils.DrawSection(
            t("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title.Inner"),
            PushDown: false,
            RespectUiTheme: !IsWindow);

        var changed = false;

        changed |= ImGui.Checkbox(t("PortraitHelperWindows.AlignmentToolSettingsOverlay.ShowAlignmentTool.Label"), ref Config.ShowAlignmentTool);

        using var _ = ImRaii.Disabled(!Config.ShowAlignmentTool);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(t("PortraitHelperWindows.AlignmentToolSettingsOverlay.VerticalLines.Label"));
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Vertical Lines", ref Config.AlignmentToolVerticalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Vertical Color", ref Config.AlignmentToolVerticalColor);

        ImGui.Unindent();
        ImGui.TextUnformatted(t("PortraitHelperWindows.AlignmentToolSettingsOverlay.HorizontalLines.Label"));
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Horizontal Lines", ref Config.AlignmentToolHorizontalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Horizontal Color", ref Config.AlignmentToolHorizontalColor);

        ImGui.Unindent();

        if (changed)
        {
            Plugin.Config.Save();
        }
    }
}
