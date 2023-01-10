using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2D8)]
public unsafe partial struct CharaViewColorant : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    // TODO: 6.3 - 0x2CC = local player id?
    [FieldOffset(0x2D0)] public bool DoUpdate;
    [FieldOffset(0x2D1)] public bool HideOtherEquipment;
    [FieldOffset(0x2D2)] public bool GearPreview;
    [FieldOffset(0x2D3)] public bool HideVisor;
    [FieldOffset(0x2D4)] public bool HideWeapon;
    [FieldOffset(0x2D5)] public bool CloseVisor;
    [FieldOffset(0x2D6)] public bool DrawWeapon;
    [FieldOffset(0x2D7)] public byte SelectedStain;

    public static CharaViewColorant* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewColorant>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 48 8B C3 C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 C7 83")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public partial void Initialize(CharaViewCharacterData* characterData, int objectIndex, IntPtr agentCallbackReady);

    [VirtualFunction(10)]
    public partial void Update(IntPtr a2, CharaViewGameObject* gameObject);
}
