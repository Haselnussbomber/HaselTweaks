using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public unsafe partial class PasteButton : MenuBarButton
{
    private readonly MenuBarState _state;
    private readonly TextService _textService;
    private readonly ClipboardService _clipboardService;
    private readonly BannerService _bannerService;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "Paste";
        Icon = FontAwesomeIcon.Paste;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.ImportFromClipboard.Label");
    }

    public override bool IsDisabled => _clipboardService.ClipboardPreset == null;

    public override void OnClick()
    {
        _bannerService.ImportPresetToState(_clipboardService.ClipboardPreset!);
        _state.CloseOverlay();
    }
}
