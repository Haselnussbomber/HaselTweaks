using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using System;

namespace HaselTweaks.Tweaks;

public unsafe class RevealDungeonRequirements : BaseTweak
{
    public override string Name => "Reveal Dungeon Requirements";

    private bool canLoad = true;
    public override bool CanLoad { get { return canLoad; } }

    [Signature("48 8B C8 48 8B D8 48 8B 10 FF 52 68 84 C0 74 1B")]
    private IntPtr Address { get; init; }
    private byte[]? OriginalBytes = null;

    private readonly int Offset = 14;
    private readonly int Length = 16;

    public override void Setup(HaselTweaks plugin)
    {
        base.Setup(plugin);

        canLoad = Address != IntPtr.Zero;

        if (CanLoad)
            PluginLog.Debug($"[RevealDungeonRequirements] Address found: {Address:X}");
        else
            PluginLog.Error("[RevealDungeonRequirements] Address not found");
    }

    public override void Enable()
    {
        base.Enable();

        if (Address == IntPtr.Zero) return;

        OriginalBytes = MemoryHelper.ReadRaw(Address + Offset, Length);

        MemoryHelper.ChangePermission(Address + Offset, Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address + Offset, new byte[] { 0x90, 0x90 }); // 2x nop to remove jz
    }

    public override void Disable()
    {
        base.Disable();

        if (Address == IntPtr.Zero) return;

        MemoryHelper.ChangePermission(Address + Offset, Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(Address + Offset, OriginalBytes!);
    }
}
