using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class FastMouseClickFix : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7"), AutoConstructIgnore]
    private nint Address { get; set; }

    public override void OnEnable()
    {
        if (Address == nint.Zero)
            _gameInteropProvider.InitializeFromAttributes(this);

        var patch = new MemoryReplacement(Address, [0x90, 0x90]); // skip jump
        patch.Enable();

        _disposables = patch;
    }

    public override void OnDisable()
    {
        DisposeAndNull(ref _disposables);
    }
}
