using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class FastMouseClickFix : ITweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    private MemoryReplacement? _patch;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7"), AutoConstructIgnore]
    private nint Address { get; init; }

    public void OnInitialize()
    {
        _gameInteropProvider.InitializeFromAttributes(this);
        _patch = new(Address, [0x90, 0x90]); // skip jump
    }

    public void OnEnable()
    {
        _patch?.Enable();
    }

    public void OnDisable()
    {
        _patch?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        _patch?.Dispose();

        Status = TweakStatus.Disposed;
    }
}
