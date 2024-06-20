using HaselTweaks.Enums.PortraitHelper;

namespace HaselTweaks.Interfaces;

public interface IOverlay : IDisposable
{
    OverlayType Type { get; }
    bool IsWindow { get; }
}
