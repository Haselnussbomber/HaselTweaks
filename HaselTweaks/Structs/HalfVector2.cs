namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x4)]
public unsafe partial struct HalfVector2
{
    [FieldOffset(0x0)] public Half X;
    [FieldOffset(0x2)] public Half Y;

    public override readonly string ToString()
        => $"HalfVector2 {{ X = {X}, Y = {Y} }}";
}
