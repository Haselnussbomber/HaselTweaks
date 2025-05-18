namespace HaselTweaks.Interfaces;

public interface ITweak : IDisposable
{
    TweakStatus Status { get; set; }
    void OnInitialize();
    void OnEnable();
    void OnDisable();
}
