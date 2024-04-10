using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

public unsafe partial struct HaselAtkFontCodeModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 85 C0 45 0F B6 C4")]
    public readonly partial uint CalculateLogLines(Utf8String* a2, Utf8String* a3, nint a4, bool a5);
}
