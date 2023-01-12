using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2D0)]
public unsafe partial struct CharaViewTryOn : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2C8)] public bool DoUpdate; // beware: fetches data from agent too, happens in vf10
    [FieldOffset(0x2C9)] public bool HideOtherEquipment;
    [FieldOffset(0x2CA)] public bool HideVisor;
    [FieldOffset(0x2CB)] public bool HideWeapon;
    [FieldOffset(0x2CC)] public bool CloseVisor;
    [FieldOffset(0x2CD)] public bool DrawWeapon;

    public static CharaViewTryOn* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewTryOn>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 48 8B C3 48 83 C4 20 5B C3 CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53 56")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(10)]
    public partial void Update(IntPtr a2, CharaViewGameObject* gameObject);
}
