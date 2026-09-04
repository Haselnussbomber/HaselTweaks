using System.Threading.Tasks;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class FastMouseClickFix : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IFramework _framework;

    [Signature("EB 3F B8 ?? ?? ?? ?? 48 8B D7"), AutoConstructIgnore]
    private nint Address { get; set; }

    public override ValueTask OnEnable()
    {
        if (Address == nint.Zero)
            _gameInteropProvider.InitializeFromAttributes(this);

        var patch = new MemoryReplacement(Address, [0x90, 0x90]); // skip jump
        _disposables = patch;

        return new ValueTask(_framework.Run(patch.Enable));
    }

    public override ValueTask OnDisable()
    {
        return new ValueTask(_framework.Run(() => DisposeAndNull(ref _disposables)));
    }
}
