using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class RevealDutyRequirements : Tweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    private MemoryReplacement? _patch;

    /*
        48 8B C8   mov     rcx, rax
        48 8B D8   mov     rbx, rax
        48 8B 10   mov     rdx, [rax]
        FF 52 70   call    qword ptr [rdx+70h]
        84 C0      test    al, al
        74 1B      jz      short loc_1409F9B09    <- removing this jz by replacing it with two nops

        that way the code doesn't jump to the else {...} which sets the duty name to "???" (Addon#102598)
     */
    [Signature("48 8B C8 48 8B D8 48 8B 10 FF 52 70 84 C0 74 1B"), AutoConstructIgnore]
    private nint Address { get; init; }

    public override void OnEnable()
    {
        if (Address == nint.Zero)
            _gameInteropProvider.InitializeFromAttributes(this);

        _patch = new(Address + 14, [0x90, 0x90]);
        _patch?.Enable();
    }

    public override void OnDisable()
    {
        _patch?.Dispose();
        _patch = null;
    }
}
