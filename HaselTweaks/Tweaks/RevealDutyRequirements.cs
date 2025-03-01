using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class RevealDutyRequirements : ITweak
{
    private readonly IGameInteropProvider _gameInteropProvider;

    private MemoryReplacement? _patch;

    public string InternalName => nameof(RevealDutyRequirements);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

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

    public void OnInitialize()
    {
        _gameInteropProvider.InitializeFromAttributes(this);
        _patch = new(Address + 14, [0x90, 0x90]);
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
