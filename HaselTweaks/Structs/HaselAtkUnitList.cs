using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x810)]
public unsafe partial struct HaselAtkUnitList
{
    [FixedSizeArray<Pointer<AtkUnitBase>>(256)]
    [FieldOffset(0x8)] public fixed byte AtkUnits[256 * 0x8];
    [FieldOffset(0x808)] public ushort Count;
}
