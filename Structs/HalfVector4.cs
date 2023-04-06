namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x8)]
public unsafe partial struct HalfVector4
{
    [FieldOffset(0x0)] public Half X;
    [FieldOffset(0x2)] public Half Y;
    [FieldOffset(0x4)] public Half Z;
    [FieldOffset(0x6)] public Half W;
}
