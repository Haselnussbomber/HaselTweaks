using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F"
[StructLayout(LayoutKind.Explicit, Size = 0x90)]
public unsafe struct CutsceneState
{
    [FieldOffset(0x00)] public void* vtbl;
    [FieldOffset(0x08)] public uint Unk1;
    [FieldOffset(0x0C)] public ushort Unk2;
    // 3 unknown/unused bytes
    [FieldOffset(0x10)] public Utf8String Path;
    [FieldOffset(0x78)] public uint Id;
    [FieldOffset(0x7C)] public uint a4;
    [FieldOffset(0x80)] public uint a5;
    [FieldOffset(0x84)] public uint a6;
    [FieldOffset(0x88)] public uint a7;
    [FieldOffset(0x8C)] public ushort Unk3;
    [FieldOffset(0x8E)] public byte a3;
    [FieldOffset(0x8F)] public byte Unk4;
}
