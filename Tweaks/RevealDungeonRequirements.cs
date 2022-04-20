using Dalamud.Logging;
using Dalamud.Memory;
using System;
using System.Linq;

namespace HaselTweaks.Tweaks;

public unsafe class RevealDungeonRequirements : BaseTweak
{
    public override string Name => "Reveal Dungeon Requirements";

    private IntPtr Address = IntPtr.Zero;
    private byte[]? OriginalBytes = null;
    private int Offset = 14;
    private int Length = 16;

    public override void Setup(HaselTweaks plugin)
    {
        base.Setup(plugin);
        Address = Service.SigScanner.ScanText("48 8B C8 48 8B D8 48 8B 10 FF 52 68 84 C0 74 1B");

        if (Address != IntPtr.Zero)
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
