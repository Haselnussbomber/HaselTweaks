using FFXIVClientStructs.FFXIV.Client.Game;

namespace HaselTweaks.Structs;

// ctor "33 D2 C7 41 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 51 08 48 89 01 48 8B C1 48 89 51 10 48 89 51 18"
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public unsafe partial struct EmoteManager
{
    public static EmoteManager* Instance() => (EmoteManager*)((nint)GameMain.Instance() + 0x3FC0);

    [MemberFunction("E8 ?? ?? ?? ?? 40 84 ED 74 18")]
    public readonly partial bool ExecuteEmote(ushort emoteId, nint targetData = 0);
}
