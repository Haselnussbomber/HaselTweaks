using System;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

public unsafe class KeepInstantPortrait : Tweak
{
    public override string Name => "Keep Instant Portrait";
    public override string Description => "Prevents Instant Portrait from being reset upon saving/updating the current Gearset.";

    /*
        8B D5               mov     edx, ebp
        49 8B CE            mov     rcx, r14
        E8 ?? ?? ?? ??      call    sub_1406664D0
        84 C0               test    al, al
        0F 84 ?? ?? ?? ??   jz      loc_140665E44     <- moving this to the start of the signature, but using a jmp rel32 instead

        completely skips the whole if () {...} block
     */
    [Signature("8B D5 49 8B CE E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 0F B6 4E 37")]
    private IntPtr Address { get; init; }
    private byte[]? OriginalBytes = null;

    private readonly int Offset = 8;

    public override void Enable()
    {
        var jmpBytes = new byte[] { 0xE9, 0x00, 0x00, 0x00, 0x00 }; // the jmp rel32
        var pos = MemoryHelper.Read<uint>(Address + 14) + 13; // address of jz adjusted to new position
        BitConverter.GetBytes(pos).CopyTo(jmpBytes, 1);

        OriginalBytes = Utils.MemoryWriteRaw(Address + Offset, jmpBytes);
    }

    public override void Disable()
    {
        Utils.MemoryWriteRaw(Address + Offset, OriginalBytes!);
    }
}
