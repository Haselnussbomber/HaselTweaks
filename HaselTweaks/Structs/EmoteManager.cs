namespace HaselTweaks.Structs;

// ctor "33 D2 C7 41 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 51 08 48 89 01 48 8B C1 48 89 51 10 48 89 51 18"
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public unsafe partial struct EmoteManager
{
    [StaticAddress("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 40 84 ED 74 18", 3)]
    public static partial EmoteManager* Instance();

    [MemberFunction("E8 ?? ?? ?? ?? 40 84 ED 74 18")]
    public partial bool ExecuteEmote(ushort emoteId, nint targetData = 0);
}
