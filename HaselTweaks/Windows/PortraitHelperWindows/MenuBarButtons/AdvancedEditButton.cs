using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public partial class AdvancedEditButton : MenuBarOverlayButton<AdvancedEditOverlay>
{
    private readonly TextService _textService;
    private readonly ClipboardService _clipboardService;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "ToggleAdvancedEditMode";
        Icon = FontAwesomeIcon.FilePen;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label");
    }

    public override bool IsDisabled => _clipboardService.ClipboardPreset == null;
}
