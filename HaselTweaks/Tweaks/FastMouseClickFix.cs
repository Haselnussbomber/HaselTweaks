using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using HaselCommon.Memory;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public sealed unsafe class FastMouseClickFix(IGameInteropProvider GameInteropProvider) : ITweak
{
    public string InternalName => nameof(FastMouseClickFix);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7")]
    private nint Address { get; init; }

    private MemoryReplacement? Patch;

    public void OnInitialize()
    {
        GameInteropProvider.InitializeFromAttributes(this);
        Patch = new(Address, [0x90, 0x90]); // skip jump
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
