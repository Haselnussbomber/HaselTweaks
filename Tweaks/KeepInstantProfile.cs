using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using System;

namespace HaselTweaks.Tweaks;

public unsafe class KeepInstantProfile : BaseTweak
{
    public override string Name => "Keep Instant Profile";

    [Signature("8B D5 49 8B CE E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 0F B6 4E 37")]
    private IntPtr Address { get; init; }
    public override bool CanLoad => Address != IntPtr.Zero;
    private byte[]? OriginalBytes = null;

    public override void Enable()
    {
        OriginalBytes = MemoryHelper.ReadRaw(Address, 5); // 5 = jmpBytes length

        var jzPos = MemoryHelper.Read<uint>(Address + 0x0E);
        jzPos += 0x0D; // position compensation

        var jmpBytes = new byte[] { 0xE9, 0x00, 0x00, 0x00, 0x00 }; // JMP rel32
        BitConverter.GetBytes(jzPos).CopyTo(jmpBytes, 1);

        MemoryHelper.ChangePermission(Address, 5, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address, jmpBytes);
    }

    public override void Disable()
    {
        MemoryHelper.ChangePermission(Address, 5, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address, OriginalBytes!);
    }
}
