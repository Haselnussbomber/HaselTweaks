using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public unsafe partial class CopyButton : MenuBarButton
{
    private readonly TextService _textService;
    private readonly ClipboardService _clipboardService;
    private readonly IFramework _framework;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "Copy";
        Icon = FontAwesomeIcon.Copy;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.ExportToClipboard.Label");
    }

    public override void OnClick()
    {
        _framework.Run(() => _clipboardService.SetClipboardPortraitPreset(PortraitPreset.FromState()));
    }
}
