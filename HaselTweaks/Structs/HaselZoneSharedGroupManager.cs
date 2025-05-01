using FFXIVClientStructs.FFXIV.Client.Game;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x738)]
public unsafe partial struct HaselZoneSharedGroupManager
{
    public static HaselZoneSharedGroupManager* Instance() => (HaselZoneSharedGroupManager*)((nint)GameMain.Instance() + 0x210);

    [MemberFunction("33 C0 4C 8B C9 38 41 32")]
    public partial void Reload();
}
