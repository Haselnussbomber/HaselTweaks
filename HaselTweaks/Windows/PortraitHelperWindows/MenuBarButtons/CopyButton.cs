using System.Threading.Tasks;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public unsafe partial class CopyButton : MenuBarButton
{
    private readonly TextService _textService;
    private readonly ClipboardService _clipboardService;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "Copy";
        Icon = FontAwesomeIcon.Copy;
        TooltipText = _textService.Translate("PortraitHelperWindows.MenuBar.ExportToClipboard.Label");
    }

    public override void OnClick()
    {
        Task.Run(() => _clipboardService.SetClipboardPortraitPreset(PortraitPreset.FromState()));
    }
}
