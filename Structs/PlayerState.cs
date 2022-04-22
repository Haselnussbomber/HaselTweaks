using System.Runtime.InteropServices;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x788)]
public unsafe struct PlayerState
{
    [FieldOffset(0x614)] public fixed byte WeeklyBonusOrderDataIds[16];
}
