using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

[RegisterSingleton, AutoConstruct]
public unsafe partial class ResetButton : MenuBarButton
{
    private readonly MenuBarState _state;
    private readonly TextService _textService;
    private readonly BannerService _bannerService;

    [AutoPostConstruct]
    private void Initialize()
    {
        Key = "Reset";
        Icon = FontAwesomeIcon.Undo;
        TooltipText = _textService.GetAddonText(4830); // Reset
    }

    public override bool IsDisabled
    {
        get
        {
            var agent = AgentBannerEditor.Instance();
            return agent == null || agent->EditorState == null || !agent->EditorState->HasDataChanged;
        }
    }

    public override void OnClick()
    {
        _bannerService.ImportPresetToState(_state.InitialPreset);

        AgentBannerEditor.Instance()->EditorState->SetHasChanged(false);
    }
}
