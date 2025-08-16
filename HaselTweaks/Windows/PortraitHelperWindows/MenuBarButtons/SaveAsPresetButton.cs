using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public unsafe partial class SaveAsPresetButton : MenuBarButton
{
    private readonly MenuBarState _state;
    private readonly TextService _textService;
    private readonly BannerService _bannerService;
    private readonly CreatePresetDialog _createPresetDialog;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "SaveAsPreset";
        Icon = FontAwesomeIcon.Download;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.SaveAsPreset.Label");
    }

    public override void OnClick()
    {
        _createPresetDialog.Open(_state.PortraitName, PortraitPreset.FromState(), _bannerService.GetCurrentCharaViewImage());
    }
}
