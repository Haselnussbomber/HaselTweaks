using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x798)]
public unsafe partial struct PlayerState
{
    [StaticAddress("4C 89 7C 24 ?? 48 8D 1D")]
    public static partial byte* GetAetherCurrentUnlocksPointer(); // UIState.Instance().PlayerState.AetherCurrentUnlocks aka. (IntPtr)UIState.Instance() + 0xA38 + 0x4E1

    // see: E8 ?? ?? ?? ?? 8B F8 85 C0 74 33
    public bool IsAetherCurrentUnlocked(uint rowId)
    {
        var id = rowId - 0x2B0000;
        var pos = id >> 3;
        var flag = (byte)(1 << (int)(id - 8 * pos));
        return (flag & GetAetherCurrentUnlocksPointer()[pos]) != 0;
    }
}
