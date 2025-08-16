using HaselTweaks.Utils.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public partial class PresetBrowserButton : MenuBarOverlayButton<PresetBrowserOverlay>
{
    private readonly TextService _textService;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "TogglePresetBrowser";
        Icon = FontAwesomeIcon.List;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label");
    }
}
