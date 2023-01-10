using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2A8)]
public unsafe partial struct CharaViewTryOn : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2A0)] public bool DoUpdate; // beware: fetches data from agent too, happens in vf10
    [FieldOffset(0x2A1)] public bool HideOtherEquipment;
    [FieldOffset(0x2A2)] public bool HideVisor;
    [FieldOffset(0x2A3)] public bool HideWeapon;
    [FieldOffset(0x2A4)] public bool CloseVisor;
    [FieldOffset(0x2A5)] public bool DrawWeapon;
    [FieldOffset(0x2A6)] public byte Unk2A6;
    [FieldOffset(0x2A7)] public byte Unk2A7;

    public static CharaViewTryOn* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewTryOn>();
    }

    // CharaView_Ctor
    [MemberFunction("E8 ?? ?? ?? ?? 41 80 A6 ?? ?? ?? ?? ?? 48 8D 05")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(10)]
    public partial void Update(IntPtr a2, CharaViewGameObject* gameObject);

    // Initialize with loading gear and some config options
    [VirtualFunction(13)]
    public partial void Vf13(IntPtr agent, int clientObjectId, IntPtr fn, bool a5);
}
