using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using HaselTweaks.Utils;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2A0)]
public unsafe partial struct CharaView : ICreatable
{
    [FieldOffset(0xC)] public uint ClientObjectId; // ClientObjectManager = non-networked objects

    [FieldOffset(0x20)] public IntPtr Camera;

    [FieldOffset(0x48)] public CharaViewCharacterData CharacterData;

    [FieldOffset(0xC2)] public float ZoomRatio;

    [FieldOffset(0xD3)] public fixed byte Items[0x20 * 14];

    public Span<CharaViewItem> CharaViewItemSpan
    {
        get
        {
            fixed (byte* ptr = Items)
            {
                return new Span<CharaViewItem>(ptr, sizeof(CharaViewItem));
            }
        }
    }

    public static CharaView* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaView>();
    }

    [MemberFunction("E8 ?? ?? ?? ?? 41 80 A6 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ??")]
    public partial void Ctor();

    [MemberFunction("40 53 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 F6 C2 01 74 0A BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B C3 48 83 C4 20 5B C3 CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC 20 8B DA 48 8B F9 E8 ?? ?? ?? ?? F6 C3 01 74 0D BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B C7 48 8B 5C 24 ?? 48 83 C4 20 5F C3 CC CC CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 8B FA 48 8B 89 ?? ?? ?? ??")]
    public partial void Dtor();

    [VirtualFunction(1)]
    public partial void Initialize(IntPtr a2, int a3, IntPtr a4);
}

[StructLayout(LayoutKind.Explicit, Size = 0x68)]
public unsafe partial struct CharaViewCharacterData : ICreatable
{
    [FieldOffset(0)] public CustomizeData CustomizeData; // see Glamourer.Customization.CharacterCustomization
    [FieldOffset(0x1A)] public byte Unk1A;
    [FieldOffset(0x1B)] public byte Unk1B;
    [FieldOffset(0x1C)] public fixed uint ItemIds[14];
    [FieldOffset(0x54)] public fixed byte ItemStains[14];
    [FieldOffset(0x62)] public byte ClassJobId;
    [FieldOffset(0x63)] public bool VisorHidden;
    [FieldOffset(0x64)] public bool WeaponHidden;
    [FieldOffset(0x65)] public bool VisorClosed;
    [FieldOffset(0x66)] public byte Unk66;
    [FieldOffset(0x67)] public byte Unk67;

    public static CharaViewCharacterData* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewCharacterData>();
    }

    [MemberFunction("E8 ?? ?? ?? ?? 4C 8D 45 10 48 8B CF")]
    public partial void Ctor();

    [MemberFunction("40 55 41 57 48 83 EC 48")]
    public partial void ImportLocalPlayerData();
}

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct CharaViewItem
{
    // there is probably some stain information etc. in here
    [FieldOffset(0x0)] public byte Unk0;
    [FieldOffset(0x1)] public short Unk1;
    [FieldOffset(0x3)] public byte Unk3;
    [FieldOffset(0x4)] public byte Unk4;
    [FieldOffset(0x5)] public byte Unk5;
    [FieldOffset(0x6)] public byte Unk6;
    [FieldOffset(0x7)] public byte Unk7;
    [FieldOffset(0x8)] public uint ItemId;
    [FieldOffset(0xC)] public byte UnkC;
    // +16 bytes
}
