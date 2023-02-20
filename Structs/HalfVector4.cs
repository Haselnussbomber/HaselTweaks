using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x8)]
public unsafe partial struct HalfVector4
{
    [FieldOffset(0x0)] public Half X;
    [FieldOffset(0x2)] public Half Y;
    [FieldOffset(0x4)] public Half Z;
    [FieldOffset(0x6)] public Half W;

    [MemberFunction("E8 ?? ?? ?? ?? 8B 7D A8")]
    public partial void ConvertFloats(float x, float y, float z, float w = 1.0f);

    public static HalfVector4* From(float x, float y, float z, float w = 1.0f)
    {
        var halfVec = (HalfVector4*)IMemorySpace.GetUISpace()->Malloc<HalfVector4>();
        halfVec->ConvertFloats(x, y, z, w);
        return halfVec;
    }

    public static HalfVector4* From(Vector3 vec)
    {
        var halfVec = (HalfVector4*)IMemorySpace.GetUISpace()->Malloc<HalfVector4>();
        halfVec->ConvertFloats(vec.X, vec.Y, vec.Z);
        return halfVec;
    }

    public static HalfVector4* From(Vector4 vec)
    {
        var halfVec = (HalfVector4*)IMemorySpace.GetUISpace()->Malloc<HalfVector4>();
        halfVec->ConvertFloats(vec.X, vec.Y, vec.Z, vec.W);
        return halfVec;
    }
}
