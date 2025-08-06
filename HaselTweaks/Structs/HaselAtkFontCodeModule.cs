using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0)]
public unsafe partial struct HaselAtkFontCodeModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 85 C0 45 0F B6 C7")]
    public partial uint CalculateLogLines(Utf8String* a2, Utf8String* a3, nint a4, bool a5);
}
