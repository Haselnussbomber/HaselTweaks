namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct InstanceAreaData
{
    [StaticAddress("48 8D 0D ?? ?? ?? ?? 0F B7 F0 E8 ?? ?? ?? ?? 8B D8")]
    public static partial InstanceAreaData* Instance();

    [MemberFunction("E8 ?? ?? ?? ?? 8B D8 3B C6")]
    public partial uint GetInstanceId();
}
