using System;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;

namespace HaselTweaks.Tweaks;

public unsafe class KeepInstantPortrait : Tweak
{
    public override string Name => "Keep Instant Portrait";
    public override string Description => "Prevents Instant Portrait from being reset upon saving/updating the current Gearset.";

    [Signature("8B D5 49 8B CE E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 0F B6 4E 37")]
    private IntPtr Address { get; init; }
    private byte[]? OriginalBytes = null;

    public override void Enable()
    {
        OriginalBytes = MemoryHelper.ReadRaw(Address, 5); // 5 = jmpBytes length

        var jmpBytes = new byte[] { 0xE9, 0x00, 0x00, 0x00, 0x00 }; // JMP rel32

        var pos = MemoryHelper.Read<uint>(Address + 0x0E) + 0x0D;
        BitConverter.GetBytes(pos).CopyTo(jmpBytes, 1);

        MemoryHelper.ChangePermission(Address, 5, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address, jmpBytes);
    }

    public override void Disable()
    {
        MemoryHelper.ChangePermission(Address, 5, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address, OriginalBytes!);
    }
}
