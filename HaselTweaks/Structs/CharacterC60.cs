using FFXIVClientStructs.FFXIV.Common.Math;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharacterC60 // Character + 0xC60
{
    [FieldOffset(0x604)] public Vector2 HeadDirection;
    [FieldOffset(0x60C)] public Vector2 EyeDirection;
}
