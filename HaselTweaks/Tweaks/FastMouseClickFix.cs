using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class FastMouseClickFix : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    private MemoryReplacement? _patch;

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7"), AutoConstructIgnore]
    private nint Address { get; set; }

    public override void OnEnable()
    {
        if (Address == nint.Zero)
            _gameInteropProvider.InitializeFromAttributes(this);

        _patch = new(Address, [0x90, 0x90]); // skip jump
        _patch.Enable();
    }

    public override void OnDisable()
    {
        _patch?.Dispose();
        _patch = null;
    }
}
