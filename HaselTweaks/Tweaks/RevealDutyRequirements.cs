using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public class RevealDutyRequirements(IGameInteropProvider GameInteropProvider) : ITweak
{
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
    [Signature("48 8B C8 48 8B D8 48 8B 10 FF 52 70 84 C0 74 1B")]
    private nint Address { get; init; }

    private MemoryReplacement? Patch;

    public void OnInitialize()
    {
        GameInteropProvider.InitializeFromAttributes(this);
        Patch = new(Address + 14, [0x90, 0x90]);
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
        if (Status == TweakStatus.Disposed)
            return;

        Patch?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }
}
