using FFXIVClientStructs.FFXIV.Client.System.Memory;
using static HaselTweaks.Structs.RaptureGearsetModule;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
public unsafe partial struct CharaViewGearset : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2A0)] public bool UpdateVisibility;
    [FieldOffset(0x2A1)] public bool UpdateItems;
    [FieldOffset(0x2A2)] public bool HideVisor;
    [FieldOffset(0x2A3)] public bool HideWeapon;
    [FieldOffset(0x2A4)] public bool CloseVisor;
    [FieldOffset(0x2A5)] public bool DrawWeapon;
    [FieldOffset(0x2A5)] public bool CharacterDisplayMode;
    [FieldOffset(0x2A5)] public byte Unk2A6;
    [FieldOffset(0x2A8)] public Gearset* Gearset;

    public static CharaViewGearset* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewGearset>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 48 8B C3 66 C7 83 ?? ?? ?? ?? ?? ?? C6 83 ?? ?? ?? ?? ?? 48 C7 83")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public partial void Initialize(CharaViewCharacterData* characterData, int objectIndex, IntPtr agentCallbackReady);

    [VirtualFunction(10)]
    public partial void Update(IntPtr a2, CharaViewGameObject* gameObject, IntPtr a4);
}
