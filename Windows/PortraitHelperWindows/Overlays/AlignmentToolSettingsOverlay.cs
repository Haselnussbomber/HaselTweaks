using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class AlignmentToolSettingsOverlay : Overlay
{
    protected override OverlayType Type => OverlayType.LeftPane;

    public AlignmentToolSettingsOverlay(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper: Alignment Tool Settings", tweak)
    {
    }

    public override void Draw()
    {
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        var style = ImGui.GetStyle();
        ImGuiUtils.TextUnformattedColored(Colors.Gold, "Alignment Tool Settings");
        ImGuiUtils.PushCursorY(-style.ItemSpacing.Y + 3);
        ImGui.Separator();
        ImGuiUtils.PushCursorY(style.ItemSpacing.Y);

        var changed = false;

        changed |= ImGui.Checkbox("Enable", ref Config.ShowAlignmentTool);

        var enabled = Config.ShowAlignmentTool;
        if (!enabled)
            ImGui.BeginDisabled();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted("Vertical Lines");
        ImGui.Indent();

        changed |= ImGui.SliderInt("##Vertical Lines", ref Config.AlignmentToolVerticalLines, 0, 10);
        changed |= ImGui.ColorEdit4("##Vertical Color", ref Config.AlignmentToolVerticalColor);

        ImGui.Unindent();
        ImGui.TextUnformatted("Horizontal Lines");
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
