using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct IslandState
{
    [StaticAddress("E8 ?? ?? ?? ?? 8B 50 10", isPointer: true)]
    public static partial IslandState* Instance();

    [FieldOffset(0x29)] public byte Level;

    [FieldOffset(0x2C)] public uint Experience;
}
