using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

// Client::Game::Event::PlayCutSceneTask
[VTableAddress("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F", 0x19 + 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x90)]
public partial struct PlayCutSceneTask
{
    [FieldOffset(0x10)] public Utf8String Path;
    [FieldOffset(0x78)] public uint Id;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B F0 48 89 45 0F")]
    public readonly partial void Ctor();
}
