using HaselTweaks.Enums;

namespace HaselTweaks.Interfaces;

public interface ITweak : IDisposable
{
    string InternalName { get; }
    TweakStatus Status { get; set; }
    void OnInitialize();
    void OnEnable();
    void OnDisable();
}
