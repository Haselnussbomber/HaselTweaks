using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using System;

namespace HaselTweaks.Tweaks;

public unsafe class RevealDungeonRequirements : BaseTweak
{
    public override string Name => "Reveal Dungeon Requirements";

    [Signature("48 8B C8 48 8B D8 48 8B 10 FF 52 68 84 C0 74 1B")]
    private IntPtr Address { get; init; }
    public override bool CanLoad => Address != IntPtr.Zero;
    private byte[]? OriginalBytes = null;

    private readonly int Offset = 14;
    private readonly int Length = 2;

    public override void Enable()
    {
        OriginalBytes = MemoryHelper.ReadRaw(Address + Offset, Length);

        MemoryHelper.ChangePermission(Address + Offset, Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address + Offset, new byte[] { 0x90, 0x90 }); // 2x nop to remove jz
    }

    public override void Disable()
    {
        MemoryHelper.ChangePermission(Address + Offset, Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address + Offset, OriginalBytes!);
    }
}
