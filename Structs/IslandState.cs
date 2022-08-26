using System.Runtime.InteropServices;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public struct IslandState
{
    [FieldOffset(0x29)] public byte Level;

    [FieldOffset(0x2C)] public uint Experience;
}
