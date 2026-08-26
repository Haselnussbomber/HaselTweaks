namespace HaselTweaks.Interfaces;

public interface ITweak : IDisposable
{
    string InternalName { get; }
    TweakStatus Status { get; set; }
    bool IsObsolete { get; set; }
    void OnEnable();
    void OnDisable();
}
