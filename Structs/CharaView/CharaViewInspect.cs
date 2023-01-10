using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
public unsafe partial struct CharaViewInspect : ICreatable
{
    [FieldOffset(0)] public CharaView Base;

    public static CharaViewInspect* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewInspect>();
    }

    // CharaView_Ctor
    [MemberFunction("E8 ?? ?? ?? ?? 41 80 A6 ?? ?? ?? ?? ?? 48 8D 05")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(7)]
    public partial byte Vf7(IntPtr a2);
}
