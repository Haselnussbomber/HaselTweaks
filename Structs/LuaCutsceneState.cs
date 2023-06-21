using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F"
[VTableAddress("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F", 0x19 + 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x90)]
public unsafe partial struct LuaCutsceneState
{
    [FieldOffset(0x10)] public Utf8String Path;
    [FieldOffset(0x78)] public uint Id;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F")]
    public partial void Ctor();
}
