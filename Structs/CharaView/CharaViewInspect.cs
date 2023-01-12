using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

// ctor is inside AgentInspect_ctor
[StructLayout(LayoutKind.Explicit, Size = 0x2C8)]
public unsafe partial struct CharaViewInspect
{
    [FieldOffset(0)] public CharaView Base;

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(7)]
    public partial byte Vf7(IntPtr a2);
}
