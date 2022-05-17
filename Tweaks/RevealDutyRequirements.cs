using System;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

public unsafe class RevealDutyRequirements : Tweak
{
    public override string Name => "Reveal Duty Requirements";
    public override string Description => "Reveals duty names in duty finder, which were shown as \"???\".\nUseful for unlocking Mentor roulette.";

    /*
        48 8B C8   mov     rcx, rax
        48 8B D8   mov     rbx, rax
        48 8B 10   mov     rdx, [rax]
        FF 52 68   call    qword ptr [rdx+68h]
        84 C0      test    al, al
        74 1B      jz      short loc_1409F9B09    <- removing this jz by replacing it with two nops

        that way the code doesn't jump to the else {...} which sets the duty name to "???" (Addon#102598)
     */
    [Signature("48 8B C8 48 8B D8 48 8B 10 FF 52 68 84 C0 74 1B")]
    private IntPtr Address { get; init; }
    private byte[]? OriginalBytes = null;

    private readonly int Offset = 14;

    public override void Enable()
    {
        OriginalBytes = Utils.MemoryReplaceRaw(Address + Offset, new byte[] { 0x90, 0x90 }); // the two nop commands
    }

    public override void Disable()
    {
        Utils.MemoryReplaceRaw(Address + Offset, OriginalBytes!);
    }
}
