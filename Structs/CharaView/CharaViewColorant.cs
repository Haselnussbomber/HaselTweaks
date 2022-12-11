using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2A8)]
public unsafe partial struct CharaViewColorant : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2A0)] public bool DoUpdate;
    [FieldOffset(0x2A1)] public bool HideOtherEquipment;
    [FieldOffset(0x2A2)] public bool GearPreview;
    [FieldOffset(0x2A3)] public bool HideVisor;
    [FieldOffset(0x2A4)] public bool HideWeapon;
    [FieldOffset(0x2A5)] public bool CloseVisor;
    [FieldOffset(0x2A6)] public bool DrawWeapon;
    [FieldOffset(0x2A7)] public byte SelectedStain;

    public static CharaViewColorant* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewColorant>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 48 8B C3 48 83 C4 20 5B C3 CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53 55")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public partial void Initialize(CharaViewCharacterData* characterData, int objectIndex, IntPtr agentCallbackReady);

    [VirtualFunction(10)]
    public partial void Update(IntPtr a2, CharaViewGameObject* gameObject);
}
