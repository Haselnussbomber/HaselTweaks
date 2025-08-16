using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public partial class AdvancedImportButton : MenuBarOverlayButton<AdvancedImportOverlay>
{
    private readonly TextService _textService;
    private readonly ClipboardService _clipboardService;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "ToggleAdvancedImportMode";
        Icon = FontAwesomeIcon.FileImport;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label");
    }

    public override bool IsDisabled => _clipboardService.ClipboardPreset == null;
}
