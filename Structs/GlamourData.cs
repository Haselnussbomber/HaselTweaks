namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x1478)]
public unsafe partial struct GlamourData
{
    [StaticAddress("48 8B 1D ?? ?? ?? ?? 48 85 DB 74 48", 3, isPointer: true)]
    public static partial GlamourData* Instance();

    [FieldOffset(0xFA8), FixedSizeArray<GlamourPlate>(20)]
    public fixed byte GlamourPlates[0x36 * 20];
    [FieldOffset(0x1458)] public bool GlamourPlatesRequested;
    [FieldOffset(0x1459)] public bool GlamourPlatesLoaded;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 47 28 48 89 5F 30")]
    public partial void RequestGcGlamours();

    [MemberFunction("E8 ?? ?? ?? ?? 32 C0 48 8B 5C 24 ?? 48 8B 6C 24 ?? 48 83 C4 20 5F")]
    public partial void RequestGlamourPlates();

    [StructLayout(LayoutKind.Explicit, Size = 0x36)]
    public struct GlamourPlate
    {
        [FieldOffset(0x00)] public fixed uint ItemIds[12];
        [FieldOffset(0x30)] public fixed byte StainIds[12];
    }
}
