using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Utils.PortraitHelper;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AlignmentToolRenderer
{
    private readonly MenuBarState _state;
    private readonly PluginConfig _pluginConfig;

    public void Draw()
    {
        var config = _pluginConfig.Tweaks.PortraitHelper;

        if (!config.ShowAlignmentTool)
            return;

        if (ImGuiHelpers.GlobalScale <= 1 && _state.Overlay is AdvancedImportOverlay or PresetBrowserOverlay)
            return;

        if (!TryGetAddon<AddonBannerEditor>(AgentId.BannerEditor, out var addon))
            return;

        var rightPanel = addon->GetNodeById(107);
        var charaView = addon->GetNodeById(130);
        var scale = addon->Scale;

        var position = new Vector2(
            addon->X + rightPanel->X * scale,
            addon->Y + rightPanel->Y * scale
        );

        var size = new Vector2(
            charaView->GetWidth() * scale,
            charaView->GetHeight() * scale
        );

        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(size);

        ImGui.Begin("AlignmentTool", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs);

        var drawList = ImGui.GetWindowDrawList();

        if (config.AlignmentToolVerticalLines > 0)
        {
            var x = size.X / (config.AlignmentToolVerticalLines + 1);

            for (var i = 1; i <= config.AlignmentToolVerticalLines + 1; i++)
            {
                drawList.AddLine(
                    position + new Vector2(i * x, 0),
                    position + new Vector2(i * x, size.Y),
                    ImGui.ColorConvertFloat4ToU32(config.AlignmentToolVerticalColor)
                );
            }
        }

        if (config.AlignmentToolHorizontalLines > 0)
        {
            var y = size.Y / (config.AlignmentToolHorizontalLines + 1);

            for (var i = 1; i <= config.AlignmentToolHorizontalLines + 1; i++)
            {
                drawList.AddLine(
                    position + new Vector2(0, i * y),
                    position + new Vector2(size.X, i * y),
                    ImGui.ColorConvertFloat4ToU32(config.AlignmentToolHorizontalColor)
                );
            }
        }

        ImGui.End();
    }
}
