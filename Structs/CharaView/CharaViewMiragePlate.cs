using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
public unsafe partial struct CharaViewMiragePlate : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2A0)] public bool DoUpdate;
    [FieldOffset(0x2A1)] public byte Unk2A1;
    [FieldOffset(0x2A2)] public byte Unk2A2;
    [FieldOffset(0x2A3)] public byte Unk2A3;
    [FieldOffset(0x2A4)] public BitVector32 Flags;
    [FieldOffset(0x2A8)] public IntPtr MiragePlate;

    public bool HideOtherEquipment
    {
        get => Flags[0];
        set => Flags[0] = value;
    }

    public bool HideVisor
    {
        get => Flags[1];
        set => Flags[1] = value;
    }

    public bool HideWeapon
    {
        get => Flags[2];
        set => Flags[2] = value;
    }

    public bool CloseVisor
    {
        get => Flags[3];
        set => Flags[3] = value;
    }

    public bool DrawWeapon
    {
        get => Flags[4];
        set => Flags[4] = value;
    }

    public static CharaViewMiragePlate* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewMiragePlate>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 66 C7 83 ?? ?? ?? ?? ?? ?? 48 89 03 33 C0 89 83 ?? ?? ?? ??")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    [VirtualFunction(1)]
    public partial void Initialize(CharaViewCharacterData* characterData, int objectIndex, IntPtr agentCallbackReady);

    [VirtualFunction(10)]
    public partial void Update(IntPtr a2, CharaViewGameObject* gameObject);
}
