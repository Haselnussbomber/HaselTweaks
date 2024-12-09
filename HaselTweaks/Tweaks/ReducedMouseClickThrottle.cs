using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using HaselCommon.Memory;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public sealed unsafe class ReducedMouseClickThrottle(IGameInteropProvider GameInteropProvider) : ITweak
{
    public string InternalName => nameof(ReducedMouseClickThrottle);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    [Signature("81 C1 2C 01 00 00 3B D9")]
    private nint Address { get; init; }

    private MemoryReplacement? Patch;

    public void OnInitialize()
    {
        GameInteropProvider.InitializeFromAttributes(this);
        Patch = new(Address + 2, [100, 00]); // 100ms
    }

    public void OnEnable()
    {
        Patch?.Enable();
    }

    public void OnDisable()
    {
        Patch?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        Patch?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }
}
