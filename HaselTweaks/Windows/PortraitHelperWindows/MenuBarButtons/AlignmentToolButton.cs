using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public partial class AlignmentToolButton : MenuBarOverlayButton<AlignmentToolSettingsOverlay>
{
    private readonly TextService _textService;
    private readonly PluginConfig _pluginConfig;
    private readonly MenuBarState _state;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "ToggleAlignmentTool";
        Icon = FontAwesomeIcon.Hashtag;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label");
    }

    public override bool IsActive => _pluginConfig.Tweaks.PortraitHelper.ShowAlignmentTool;

    public override void OnClick()
    {
        if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
        {
            if (_state.Overlay is AlignmentToolSettingsOverlay)
            {
                _state.CloseOverlay();
            }
            else
            {
                _state.OpenOverlay<AlignmentToolSettingsOverlay>();
            }
        }
        else
        {
            _pluginConfig.Tweaks.PortraitHelper.ShowAlignmentTool = !_pluginConfig.Tweaks.PortraitHelper.ShowAlignmentTool;
            _pluginConfig.Save();
        }
    }
}
