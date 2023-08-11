using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AlignmentToolSettingsOverlay : Overlay
{
    protected override OverlayType Type => OverlayType.LeftPane;

    public AlignmentToolSettingsOverlay(PortraitHelper tweak) : base(t("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title"), tweak)
    {
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        var style = ImGui.GetStyle();
        ImGuiUtils.TextUnformattedColored(Colors.Gold, t("PortraitHelperWindows.AlignmentToolSettingsOverlay.Title.Inner"));
        ImGuiUtils.PushCursorY(-style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGuiUtils.PushCursorY(style.ItemSpacing.Y);

        var changed = false;

        changed |= ImGui.Checkbox(t("PortraitHelperWindows.AlignmentToolSettingsOverlay.ShowAlignmentTool.Label"), ref Config.ShowAlignmentTool);

        var enabled = Config.ShowAlignmentTool;
        if (!enabled)
            ImGui.BeginDisabled();

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

        if (!enabled)
            ImGui.EndDisabled();
    }
}
