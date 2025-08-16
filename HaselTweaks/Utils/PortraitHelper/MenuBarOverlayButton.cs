using HaselTweaks.Services.PortraitHelper;

namespace HaselTweaks.Utils.PortraitHelper;

[AutoConstruct]
public partial class MenuBarOverlayButton<T> : MenuBarButton where T : IOverlay
{
    private readonly MenuBarState _state;

    public override bool IsActive => _state.Overlay is T;

    public override void OnClick()
    {
        if (IsActive)
        {
            _state.CloseOverlay();
        }
        else
        {
            _state.OpenOverlay<T>();
        }
    }
}
